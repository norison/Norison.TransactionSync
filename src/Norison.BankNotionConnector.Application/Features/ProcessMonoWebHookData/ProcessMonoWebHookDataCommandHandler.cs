using MediatR;

using Norison.BankNotionConnector.Persistence.Storages;
using Norison.BankNotionConnector.Persistence.Storages.Models;

using Notion.Client;

using Telegram.Bot;

namespace Norison.BankNotionConnector.Application.Features.ProcessMonoWebHookData;

public class ProcessMonoWebHookDataCommandHandler(IStorageFactory storageFactory, ITelegramBotClient client)
    : IRequestHandler<ProcessMonoWebHookDataCommand>
{
    public async Task Handle(ProcessMonoWebHookDataCommand request, CancellationToken cancellationToken)
    {
        var userStorage = storageFactory.GetUsersStorage();

        var user = await userStorage.GetFirstAsync(
            new DatabasesQueryParameters { Filter = new NumberFilter("ChatId", request.ChatId) }, cancellationToken);

        if (user is null)
        {
            return;
        }

        var statement = request.WebHookData.StatementItem;

        try
        {
            var accountsStorage = storageFactory.GetAccountsStorage(user.NotionToken);
            var transactionsStorage = storageFactory.GetTransactionsStorage(user.NotionToken);
            var budgetsStorage = storageFactory.GetBudgetsStorage(user.NotionToken);

            var account = await accountsStorage.GetFirstAsync(
                new DatabasesQueryParameters { Filter = new TitleFilter("Name", user.MonoAccountName) },
                cancellationToken);

            if (account is null)
            {
                await client.SendTextMessageAsync(request.ChatId,
                    $"Transaction '{statement.Description}' was not added. Account was not found. Use /setsettings to set them.",
                    cancellationToken: cancellationToken);
                return;
            }

            var budget = await budgetsStorage.GetFirstAsync(new DatabasesQueryParameters(), cancellationToken);

            var type = statement.Amount > 0 ? TransactionType.Income : TransactionType.Expense;
            
            var amount = Math.Abs(statement.Amount / 100);

            var newTransaction = new TransactionDbModel
            {
                Name = statement.Description,
                Type = type,
                Date = statement.Time,
                AmountFrom = type == TransactionType.Expense ? amount : null,
                AmountTo = type == TransactionType.Income ? amount : null,
                Notes = statement.Comment,
                AccountFromIds = type == TransactionType.Expense ? [account.Id!] : [],
                AccountToIds = type == TransactionType.Income ? [account.Id!] : [],
                CategoryIds = [],
                BudgetIds = budget is null ? [] : [budget.Id!]
            };

            await transactionsStorage.AddAsync(newTransaction, cancellationToken);

            await client.SendTextMessageAsync(request.ChatId,
                $"Transaction '{statement.Description}' was added successfully.", cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            await client.SendTextMessageAsync(request.ChatId,
                $"Transaction '{statement.Description}' was not added. Error: {exception.Message}",
                cancellationToken: cancellationToken);
        }
    }
}