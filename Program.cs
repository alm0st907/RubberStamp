// See https://aka.ms/new-console-template for more information

///
/// TODOS:
/// - change to normal 'main' format instead of this script style
/// - clean up the code...looks like a personal project (it is)
/// - nail down the flow
///         - pick budget or use last budget
///         - get unapproved transactions per account
///         - prompt for bulk approval
///         - submit bulk approval
/// 

using System.Net;
using System.Net.Http.Headers;
using Spectre.Console;
using YnabClientSpace;
using Type = Ynab.Outputs.Type;

var client = new HttpClient();
var pat = System.Environment.GetEnvironmentVariable("YNAB_PAT");
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", pat);

const string baseAddr = "https://api.ynab.com/v1/";
var ynabClient = new YnabClient(client);
ynabClient.BaseUrl = baseAddr;

var res = await client.GetAsync(baseAddr + "user");

AnsiConsole.WriteLine($"Request was {(res.StatusCode == HttpStatusCode.OK ? "successful" : "unsuccessful")}");
if (res.StatusCode == HttpStatusCode.OK)
{
    AnsiConsole.WriteLine("Able to reach YNAB: Continuing");
}
else
{
    AnsiConsole.WriteLine("Unable to reach ynab, exiting");
    return;
}

//do some fancy redirect logic later if I feel like it
if (Console.IsOutputRedirected)
{
    AnsiConsole.WriteLine("Output is redirected, exiting");
    return;
}

res = await client.GetAsync(baseAddr + "budgets");

var budgets = await ynabClient.GetBudgetsAsync(false);
var names = budgets?.Data?.Budgets.Select(x => x.Name).ToList();

if (names != null)
{
    //store last used budget somewhere
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>().Title("Budgets to choose from")
            .PageSize(10)
            .AddChoices(names));
    AnsiConsole.WriteLine($"This is your Selection:\n{choice}");
    var choiceId = budgets!.Data!.Budgets.Where(x => x.Name == choice).Select(x => x.Id).First();

    var budget = await ynabClient.GetBudgetByIdAsync(choiceId.ToString(), null);

    AnsiConsole.WriteLine($"You fetched the budget {budget.Data.Budget.Name}");

    AnsiConsole.Clear();
    var transactions = await ynabClient.GetTransactionsAsync(choiceId.ToString(), null, Type.Unapproved, null);
    AnsiConsole.WriteLine($"Current transactions:");
    foreach (var tx in transactions.Data.Transactions)
    {
        //format amount into currency
        var correctedAmount = tx.Amount / 10;
        var preDecimal = correctedAmount.ToString().Insert(0, "$");
        var formattedAmount = preDecimal.Insert(preDecimal.Length - 2, ".");


        AnsiConsole.WriteLine($"{tx.Date.ToString("MM/dd/yyyy")}" + " " + tx.Payee_name + " " + tx.Account_name + " " +
                              formattedAmount + " " + tx.Category_name);
    }
}