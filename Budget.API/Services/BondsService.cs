using Budget.API.Helpers;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Diagnostics;
using System.Text.Json;
using Budget.API.Models.DbModels.Bonds.API;
using Budget.API.Models.DbModels.Bonds;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Text;

namespace Budget.API.Services;

public class BondsService
{
    private readonly IDbContextFactory<BondsDbContext> _dbContext;

    public BondsService(IDbContextFactory<BondsDbContext> dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GetBondsStatus()
    {
        // activesAmount \t isin \t end date \t days left \t price \t cost 

        var now = DateTime.UtcNow;
        var nowDateOnly = DateOnly.FromDateTime(now);

        using (var db = await _dbContext.CreateDbContextAsync())
        {
            var bonds = await db.Bonds.AsNoTracking()
                                      .Where(x => x.DateEnd >= now && x.Amount != 0)
                                      .Include(x => x.CostHistory.Where(h => h.Date == nowDateOnly))
                                      .Include(x => x.Actives)
                                      .ToListAsync();

            //if (bonds.Any(x => x.CostHistory.Count == 0))
            //{
            //    // no history for today (FIX FOR HOLIDAYS)
            //    return string.Empty;
            //}

            List<string> result = new();

            foreach (var item in bonds.OrderBy(x => x.DateEnd))
            {
                var history = item.CostHistory.FirstOrDefault();
                if (history == null)
                    continue;

                string price = history.Cost.ToString(CultureInfo.InvariantCulture);
                string cost = item.Cost.ToString(CultureInfo.InvariantCulture);
                
                result.Add($"{item.Actives.Sum(x => x.Amount)}|{item.Isin}|{item.DateEnd.ToString("dd.MM.yyyy")}|{(int)Math.Ceiling((item.DateEnd - now).TotalDays)}|{price}|{cost}");
            }

            return string.Join('\n', result);
        }
    }

    public async Task<(int, int)> UpdateBonds(string xRef, string cookie)
    {
        int newBondsCount = 0, updatedBondsCount = 0;

        List<GetAllBondsApiModel> apiBonds = null;

        string endpoint = $"https://next.privat24.ua/api/p24/bonds?currency=UAH&xref={xRef}&action=price";

        using (var client = PrepareHttpClient(cookie))
        {
            var response = await client.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                Debug.WriteLine(err);
                return (-1, -1);
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<GetAllBondsApiModelRequest>(content)!;

            apiBonds = json.data.OrderBy(x => x.type).ThenBy(x => DateTime.Parse(x.dend, CultureInfo.InvariantCulture)).ToList();

            // Create bonds if new
            using (var db = await _dbContext.CreateDbContextAsync())
            {
                var bonds = await db.Bonds.AsNoTracking()
                                            .Select(x => x.Isin)
                                            .ToListAsync();

                var newBonds = new List<BondDbModel>();
                foreach (var item in apiBonds)
                {
                    if (bonds.Contains(item.isin))
                        continue;

                    var cost = await GetBondCost(client, xRef, item.isin);

                    var newBond = new BondDbModel
                    {
                        Isin = item.isin,
                        DateEnd = DateTime.Parse(item.dend, CultureInfo.InvariantCulture),
                        Type = item.type == "SIM" ? 0 : 1,
                        Cost = cost
                    };

                    if (item.type == "YTM")
                    {
                        var payments = await GetBondCouponPayments(client, xRef, item.isin);

                        newBond.Payments = payments;
                    }
                    else
                    {
                        newBond.Payments = new List<PaymentDbModel>()
                        {
                            new PaymentDbModel()
                            {
                                Bond = newBond,
                                Payment = cost,
                                Date = DateTime.Parse(item.dend, CultureInfo.InvariantCulture)
                            }
                        };
                    }

                    newBonds.Add(newBond);
                    newBondsCount++;
                }
                if (newBonds.Any())
                {
                    await db.Bonds.AddRangeAsync(newBonds);
                    await db.SaveChangesAsync();
                }
            }
        }

        List<CostHistoryDbModel> newCostHistory = new();

        using (var db = await _dbContext.CreateDbContextAsync())
        {
            var dbBonds = await db.Bonds.AsNoTracking()
                                        .Include(x => x.CostHistory)
                                        .ToListAsync();

            // Check for amount and price of bonds
            using (var client = PrepareHttpClient(cookie))
            {
                foreach (var bond in dbBonds)
                {
                    await GetBondAmount(client, xRef, bond);
                    if(bond.Amount != 0)
                        await GetBondPrice(client, xRef, bond, newCostHistory);

                    db.Entry(bond).State = EntityState.Modified;
                }
            }

            foreach(var item in newCostHistory)
            {
                var prevElement = item.Bond.CostHistory.LastOrDefault();
                if (prevElement == null || prevElement.Date.Day != item.Date.Day)
                {
                    await db.CostHistory.AddAsync(item);
                    updatedBondsCount++;
                }
            }

            await db.SaveChangesAsync();
        }

        return (newBondsCount, updatedBondsCount);
    }

    private async Task<decimal> GetBondCost(HttpClient client, string xRef, string isin)
    {
        string endpoint = "https://next.privat24.ua/api/p24/bonds";

        dynamic payload = new { isin = isin, count = 1, source = "3", xref = xRef, action = "commissions" };
        var body = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await client.PostAsync(endpoint, body);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return 0.0m;
        }

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<BondCostApiModelResponse>(content)!;

        return decimal.Parse(json.data[0].expectedPaymentsAmount, CultureInfo.InvariantCulture);
    }

    private async Task GetBondAmount(HttpClient client, string xref, BondDbModel dbModel)
    {
        string endpoint = $"https://next.privat24.ua/api/p24/bonds?isin={dbModel.Isin}&operation=s&xref={xref}&action=limits";

        var response = await client.GetAsync(endpoint);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<BondAmountApiModelResponse>(content)!;

        dbModel.Amount = int.Parse(json.data.count);
        //dbModel.IsClosed = int.Parse(json.data.active ? "0" : "1");
    }

    private async Task GetBondPrice(HttpClient client, string xref, BondDbModel dbModel, List<CostHistoryDbModel> costHistory)
    {
        string endpoint = $"https://next.privat24.ua/api/p24/bonds?isin={dbModel.Isin}&action=info&xref={xref}";

        var response = await client.GetAsync(endpoint);

        if (response.IsSuccessStatusCode == false)
        {
            var err = await response.Content.ReadAsStringAsync();
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<BondPriceApiModelResponse>(content)!;

        if (!json.data.Any())
            return;

        costHistory.Add(new CostHistoryDbModel()
        {
            BondId = dbModel.Id,
            Bond = dbModel,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Cost = decimal.Parse(json.data[0].askPrice, CultureInfo.InvariantCulture)
        });
    }

    private async Task<List<PaymentDbModel>> GetBondCouponPayments(HttpClient client, string xRef, string isin)
    {
        List<PaymentDbModel> payments = new();

        // https://next.privat24.ua/api/p24/bonds?isin=UA4000228449&xref=268fe6afebb92fcc4e8542a12638cba1&action=operations
        string endpoint = $"https://next.privat24.ua/api/p24/bonds?isin={isin}&xref={xRef}&action=operations";

        var response = await client.GetAsync(endpoint);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<BondCouponPaymentsApiModelResponse>(content)!;

        var activeCoupons = json.data.Where(x => DateTime.Parse(x.dat) > DateTime.UtcNow).ToList();

        foreach(var item in activeCoupons)
        {
            payments.Add(new PaymentDbModel()
            {
                Payment = decimal.Parse(item.kupon, CultureInfo.InvariantCulture) + decimal.Parse(item.payment, CultureInfo.InvariantCulture),
                Date = DateTime.Parse(item.dat, CultureInfo.InvariantCulture)
            });
        }

        return payments;
    }

    public async Task<string> GetPaymentDate()
    {
        List<BondPayment> payments = new();



        using (var db = await _dbContext.CreateDbContextAsync())
        {
            var actives = await db.Actives.AsNoTracking()
                                          .Where(x => x.IsClosed == 0)
                                          .Include(x => x.Bond)
                                          .ThenInclude(x => x.Payments)
                                          .OrderByDescending(x => x.DateBuy)
                                          .ToListAsync();

            foreach (var item in actives)
            {
                if (!item.Bond.Payments.Any())
                    continue;

                foreach(var pay in item.Bond.Payments)
                {
                    payments.Add(new(pay.Date, pay.Payment, item.Bond, item.Amount, item.Price * item.Amount));
                }
            }
        }

        var meow = payments.OrderBy(x => x.Date).GroupBy(x => x.Date).ToList();

        string result = "";

        var now = DateTime.Now;

        foreach (var item in meow)
        {
            decimal buyPrice = item.Sum(x => x.Price);
            decimal sellPrice = item.Sum(x => x.amount * x.Payment);
            string profit = (sellPrice - buyPrice).ToString(CultureInfo.InvariantCulture);
            var text = string.Join(',', item.Select(x => x.Bond.Isin).Distinct());
            string type = item.First().Bond.Type == 0 ? "SIM" : "YTM";

            if(type == "YTM")
            {
                buyPrice = 0;
                profit = "";
            }

            result += $"{(int)Math.Ceiling((item.Key - now).TotalDays)}|{item.Key.ToString("dd.MM.yyyy")}|{sellPrice.ToString(CultureInfo.InvariantCulture)}|{buyPrice.ToString(CultureInfo.InvariantCulture)}|{profit}|{text}|{type}\n";
        }

        return result;
    }

    record BondPayment(DateTime Date, decimal Payment, BondDbModel Bond, int amount, decimal Price);

    // Money Graph 
    private async Task Meow()
    {
        using (var db = await _dbContext.CreateDbContextAsync())
        {
            //var bonds = await db.Bonds.AsNoTracking().ToListAsync();

            var actives = await db.Actives.AsNoTracking()
                                          .Include(x => x.Bond)
                                          .ToListAsync();

            HashSet<DateTime> datePoints = new();

            foreach(var item in actives)
            {
                datePoints.Add(item.DateBuy);
                datePoints.Add(item.Bond.DateEnd);
            }

            var datePointsList = datePoints.Order().ToList();

            List<result> results = new();

            foreach (var item in datePointsList)
            {
                decimal sum = 0.0m;

                foreach(var act in actives.Where(x => x.DateBuy <= item && x.Bond.DateEnd >= item))
                {
                    sum += act.Price * act.Amount;
                }

                results.Add(new result(DateOnly.FromDateTime(item), sum));
            }

            var text = string.Join('\n', results.Select(x => $"{x.Date.Month}/{x.Date.Day}/{x.Date.Year}\t{x.Value.ToString(CultureInfo.InvariantCulture)}"));
        }
    }

    record result (DateOnly Date, decimal Value);

    private HttpClient PrepareHttpClient(string cookieRaw)
    {
        cookieRaw = cookieRaw.Replace(",", "");

        var baseAddress = new Uri("https://next.privat24.ua");

        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
        var client = new HttpClient(handler);
        foreach (var item in cookieRaw.Split("; "))
        {
            var parts = item.Split("=");
            cookieContainer.Add(baseAddress, new Cookie(parts[0], parts[1]));
        }

        return client;
    }
}
