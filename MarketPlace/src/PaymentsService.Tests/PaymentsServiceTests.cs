using Xunit;
using PaymentsService.Controllers;
using PaymentsService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace PaymentsService.Tests
{
    public class AccountsControllerTests
    {
        private PaymentsDbContext GetTestDatabase()
        {
            var options = new DbContextOptionsBuilder<PaymentsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new PaymentsDbContext(options);
            return context;
        }

        [Fact]
        public async Task CreateAccount_ReturnsOk_WhenAccountIsCreated()
        {
            var dbContext = GetTestDatabase();
            var controller = new AccountsController(dbContext);
            var userId = "testUser456";
            
            var result = await controller.CreateAccount(new CreateAccountRequest { UserId = userId });
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var account = Assert.IsType<Account>(okResult.Value);
            Assert.Equal(userId, account.UserId);
            Assert.Equal(0, account.Balance);
            
            var storedAccount = await dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            Assert.NotNull(storedAccount);
            Assert.Equal(0, storedAccount.Balance);
        }

        [Fact]
        public async Task CreateAccount_StoresAccountInDatabase()
        {
            var dbContext = GetTestDatabase();
            var controller = new AccountsController(dbContext);
            var userId = "testUserForDatabase";
            
            var result = await controller.CreateAccount(new CreateAccountRequest { UserId = userId });
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var account = Assert.IsType<Account>(okResult.Value);
            
            var storedAccount = await dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            Assert.NotNull(storedAccount);
            Assert.Equal(userId, storedAccount.UserId);
            Assert.Equal(0, storedAccount.Balance);
        }

        [Fact]
        public async Task CreateAccount_ReturnsConflict_WhenAccountAlreadyExists()
        {
            var dbContext = GetTestDatabase();
            var userId = "existingUser";
            var existingAccount = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = 50
            };
            
            dbContext.Accounts.Add(existingAccount);
            await dbContext.SaveChangesAsync();
            
            var controller = new AccountsController(dbContext);
            
            var result = await controller.CreateAccount(new CreateAccountRequest { UserId = userId });
            
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Contains("already exists", conflictResult.Value.ToString());
        }

        [Fact]
        public async Task Deposit_UpdatesBalance_WhenAccountExists()
        {
            var dbContext = GetTestDatabase();
            var userId = "depositUser";
            var initialBalance = 200.75m;
            var depositAmount = 150.25m;
            
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = initialBalance
            };
            
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();
            
            var controller = new AccountsController(dbContext);
            var depositRequest = new DepositRequest
            {
                UserId = userId,
                Amount = depositAmount
            };
            
            var result = await controller.Deposit(depositRequest);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedAccount = Assert.IsType<Account>(okResult.Value);
            Assert.Equal(userId, updatedAccount.UserId);
            Assert.Equal(initialBalance + depositAmount, updatedAccount.Balance);
            
            var dbAccount = await dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            Assert.NotNull(dbAccount);
            Assert.Equal(initialBalance + depositAmount, dbAccount.Balance);
        }

        [Fact]
        public async Task Deposit_HandlesPossibleValidation()
        {
            var dbContext = GetTestDatabase();
            var userId = "validationTestUser";
            var initialBalance = 100m;
            var depositAmount = 50m;
            
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = initialBalance
            };
            
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();
            
            var controller = new AccountsController(dbContext);
            var depositRequest = new DepositRequest
            {
                UserId = userId,
                Amount = depositAmount
            };
            
            
            var result = await controller.Deposit(depositRequest);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedAccount = Assert.IsType<Account>(okResult.Value);
            Assert.Equal(initialBalance + depositAmount, returnedAccount.Balance);
        }

        [Fact]
        public async Task Deposit_ReturnsNotFound_WhenAccountDoesNotExist()
        {
            var dbContext = GetTestDatabase();
            var controller = new AccountsController(dbContext);
            var depositRequest = new DepositRequest
            {
                UserId = "nonExistentUser",
                Amount = 50
            };
            
            var result = await controller.Deposit(depositRequest);
            
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetBalance_ReturnsBalance_WhenAccountExists()
        {
            var dbContext = GetTestDatabase();
            var userId = "balanceUser";
            var expectedBalance = 325.50m;
            
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = expectedBalance
            };
            
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();
            
            var controller = new AccountsController(dbContext);
            
            var result = await controller.GetBalance(userId);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var json = JsonSerializer.Serialize(okResult.Value);
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
            
            Assert.NotNull(values);
            Assert.True(values.ContainsKey("UserId"));
            Assert.True(values.ContainsKey("Balance"));
            
            Assert.Equal(userId, values["UserId"].GetString());
            Assert.Equal(expectedBalance, values["Balance"].GetDecimal());
        }

        [Fact]
        public async Task GetBalance_ReturnsNotFound_WhenAccountDoesNotExist()
        {
            var dbContext = GetTestDatabase();
            var controller = new AccountsController(dbContext);
            
            var result = await controller.GetBalance("unknownUser");
            
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateAccount_AssignsUniqueId_ForEachAccount()
        {
            var dbContext = GetTestDatabase();
            var controller = new AccountsController(dbContext);
            var userId1 = "uniqueIdUser1";
            var userId2 = "uniqueIdUser2";
            
            var result1 = await controller.CreateAccount(new CreateAccountRequest { UserId = userId1 });
            var result2 = await controller.CreateAccount(new CreateAccountRequest { UserId = userId2 });
            
            var okResult1 = Assert.IsType<OkObjectResult>(result1);
            var account1 = Assert.IsType<Account>(okResult1.Value);

            var okResult2 = Assert.IsType<OkObjectResult>(result2);
            var account2 = Assert.IsType<Account>(okResult2.Value);

            Assert.NotEqual(account1.Id, account2.Id);
        }

        [Fact]
        public async Task Deposit_WithNegativeAmount_StillProcessesDeposit()
        {
            var dbContext = GetTestDatabase();
            var userId = "negativeDepositUser";
            var initialBalance = 500m;
            var depositAmount = -100m;
            
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = initialBalance
            };
            
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();
            
            var controller = new AccountsController(dbContext);
            var depositRequest = new DepositRequest
            {
                UserId = userId,
                Amount = depositAmount
            };
            
            var result = await controller.Deposit(depositRequest);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedAccount = Assert.IsType<Account>(okResult.Value);
            Assert.Equal(initialBalance + depositAmount, updatedAccount.Balance);
            
            var dbAccount = await dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            Assert.NotNull(dbAccount);
            Assert.Equal(initialBalance + depositAmount, dbAccount.Balance);
        }

        [Fact]
        public async Task Deposit_WithZeroAmount_DoesNotChangeBalance()
        {
            var dbContext = GetTestDatabase();
            var userId = "zeroDepositUser";
            var initialBalance = 250m;
            var depositAmount = 0m;
            
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = initialBalance
            };
            
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();
            
            var controller = new AccountsController(dbContext);
            var depositRequest = new DepositRequest
            {
                UserId = userId,
                Amount = depositAmount
            };
            
            var result = await controller.Deposit(depositRequest);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedAccount = Assert.IsType<Account>(okResult.Value);
            Assert.Equal(initialBalance, updatedAccount.Balance);
        }

        [Fact]
        public async Task Deposit_WithLargeAmount_HandlesCorrectly()
        {
            var dbContext = GetTestDatabase();
            var userId = "largeDepositUser";
            var initialBalance = 1000m;
            var depositAmount = 9999999.99m;
            
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = initialBalance
            };
            
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();
            
            var controller = new AccountsController(dbContext);
            var depositRequest = new DepositRequest
            {
                UserId = userId,
                Amount = depositAmount
            };
            
            var result = await controller.Deposit(depositRequest);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedAccount = Assert.IsType<Account>(okResult.Value);
            Assert.Equal(initialBalance + depositAmount, updatedAccount.Balance);
        }

        [Fact]
        public async Task MultipleDeposits_AccumulatesBalance_Correctly()
        {
            var dbContext = GetTestDatabase();
            var userId = "multipleDepositsUser";
            var initialBalance = 100m;
            
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = initialBalance
            };
            
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();
            
            var controller = new AccountsController(dbContext);
            
            var depositRequest1 = new DepositRequest { UserId = userId, Amount = 50m };
            var depositRequest2 = new DepositRequest { UserId = userId, Amount = 75.5m };
            var depositRequest3 = new DepositRequest { UserId = userId, Amount = 25.25m };
            
            await controller.Deposit(depositRequest1);
            await controller.Deposit(depositRequest2);
            var finalResult = await controller.Deposit(depositRequest3);
            
            var okResult = Assert.IsType<OkObjectResult>(finalResult);
            var updatedAccount = Assert.IsType<Account>(okResult.Value);
            
            decimal expectedFinalBalance = initialBalance + 50m + 75.5m + 25.25m;
            Assert.Equal(expectedFinalBalance, updatedAccount.Balance);
            
            var dbAccount = await dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            Assert.NotNull(dbAccount);
            Assert.Equal(expectedFinalBalance, dbAccount.Balance);
        }

        [Fact]
        public async Task CreateAccount_GeneratesExpectedGuidFormat()
        {
            var dbContext = GetTestDatabase();
            var controller = new AccountsController(dbContext);
            var userId = "guidTestUser";
            
            var result = await controller.CreateAccount(new CreateAccountRequest { UserId = userId });
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var account = Assert.IsType<Account>(okResult.Value);
            
            Assert.True(Guid.TryParse(account.Id.ToString(), out _));
        }

        [Fact]
        public async Task GetBalance_WithMultipleAccounts_ReturnsCorrectAccountBalance()
        {
            var dbContext = GetTestDatabase();
            var userId1 = "multiAccountUser1";
            var userId2 = "multiAccountUser2";
            var balance1 = 500m;
            var balance2 = 1000m;
            
            var accounts = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = userId1, Balance = balance1 },
                new Account { Id = Guid.NewGuid(), UserId = userId2, Balance = balance2 }
            };
            
            dbContext.Accounts.AddRange(accounts);
            await dbContext.SaveChangesAsync();
            
            var controller = new AccountsController(dbContext);
            
            var result = await controller.GetBalance(userId1);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var json = JsonSerializer.Serialize(okResult.Value);
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
            
            Assert.NotNull(values);
            Assert.Equal(userId1, values["UserId"].GetString());
            Assert.Equal(balance1, values["Balance"].GetDecimal());
        }

        [Fact]
        public async Task CreateAccount_WithEmptyUserId_StillCreatesAccount()
        {
            var dbContext = GetTestDatabase();
            var controller = new AccountsController(dbContext);
            var emptyUserId = string.Empty;
            
            var result = await controller.CreateAccount(new CreateAccountRequest { UserId = emptyUserId });
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var account = Assert.IsType<Account>(okResult.Value);
            Assert.Equal(emptyUserId, account.UserId);
            
            var storedAccount = await dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == emptyUserId);
            Assert.NotNull(storedAccount);
        }

        [Fact]
        public async Task Deposit_WithMultipleTransactions_ReturnsMostRecentBalanceState()
        {
            var dbContext = GetTestDatabase();
            var userId = "transactionsTestUser";
            var initialBalance = 1000m;
            
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = initialBalance
            };
            
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();
            
            var controller = new AccountsController(dbContext);
            
            await controller.Deposit(new DepositRequest { UserId = userId, Amount = 200m });
            await controller.Deposit(new DepositRequest { UserId = userId, Amount = -300m });
            var finalResult = await controller.Deposit(new DepositRequest { UserId = userId, Amount = 50m });
            
            decimal expectedBalance = 950m;
            
            var okResult = Assert.IsType<OkObjectResult>(finalResult);
            var updatedAccount = Assert.IsType<Account>(okResult.Value);
            Assert.Equal(expectedBalance, updatedAccount.Balance);
            
            var dbAccount = await dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            Assert.NotNull(dbAccount);
            Assert.Equal(expectedBalance, dbAccount.Balance);
        }

        [Fact]
        public async Task GetBalance_AfterMultipleAccountCreations_ReturnsCorrectUserAccount()
        {
            var dbContext = GetTestDatabase();
            var userIdToCheck = "lastCreatedUser";
            var expectedBalance = 0m;
            
            await dbContext.Accounts.AddRangeAsync(new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = "firstUser", Balance = 100m },
                new Account { Id = Guid.NewGuid(), UserId = "secondUser", Balance = 200m },
                new Account { Id = Guid.NewGuid(), UserId = userIdToCheck, Balance = expectedBalance },
            });
            await dbContext.SaveChangesAsync();
            
            var controller = new AccountsController(dbContext);
            
            var result = await controller.GetBalance(userIdToCheck);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var json = JsonSerializer.Serialize(okResult.Value);
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
            
            Assert.Equal(userIdToCheck, values["UserId"].GetString());
            Assert.Equal(expectedBalance, values["Balance"].GetDecimal());
        }
    }
}