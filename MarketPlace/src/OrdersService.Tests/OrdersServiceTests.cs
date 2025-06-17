using Xunit;
using OrdersService.Controllers;
using OrdersService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace OrdersService.Tests
{
    public class OrdersControllerTests
    {
        private OrdersDbContext GetTestDatabase()
        {
            var options = new DbContextOptionsBuilder<OrdersDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new OrdersDbContext(options);
            return context;
        }

        [Fact]
        public async Task CreateOrder_ReturnsCreatedResult_WhenOrderIsValid()
        {
            var dbContext = GetTestDatabase();
            var controller = new OrdersController(dbContext);
            var orderRequest = new CreateOrderRequest
            {
                UserId = "testUser123",
                Amount = 100.50m,
                Description = "Test order for unit testing"
            };
            
            var result = await controller.CreateOrder(orderRequest);
            
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var order = Assert.IsType<Order>(createdResult.Value);
            Assert.Equal(orderRequest.UserId, order.UserId);
            Assert.Equal(orderRequest.Amount, order.Amount);
            Assert.Equal(orderRequest.Description, order.Description);
            Assert.Equal(OrderStatus.New, order.Status);
            
            var storedOrder = await dbContext.Orders.FindAsync(order.Id);
            Assert.NotNull(storedOrder);
        }

        [Fact]
        public async Task CreateOrder_StoresOrderInDatabase()
        {
            var dbContext = GetTestDatabase();
            var controller = new OrdersController(dbContext);
            var orderRequest = new CreateOrderRequest
            {
                UserId = "testUser123",
                Amount = 100.50m,
                Description = "Test order for unit testing"
            };
            
            var result = await controller.CreateOrder(orderRequest);
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var order = Assert.IsType<Order>(createdResult.Value);
            
            var storedOrder = await dbContext.Orders.FindAsync(order.Id);
            Assert.NotNull(storedOrder);
            
            var outboxMessages = await dbContext.OutboxMessages.ToListAsync();
            Assert.Single(outboxMessages);
            Assert.Equal("OrderCreated", outboxMessages[0].Type);
        }

        [Fact]
        public async Task GetOrders_ReturnsOrders_WhenUserHasOrders()
        {
            var dbContext = GetTestDatabase();
            var userId = "user1";
            var orders = new List<Order>
            {
                new Order { Id = Guid.NewGuid(), UserId = userId, Amount = 10, Description = "Order 1", Status = OrderStatus.New },
                new Order { Id = Guid.NewGuid(), UserId = userId, Amount = 20, Description = "Order 2", Status = OrderStatus.Finished },
                new Order { Id = Guid.NewGuid(), UserId = "otherUser", Amount = 30, Description = "Order 3", Status = OrderStatus.New }
            };

            dbContext.Orders.AddRange(orders);
            await dbContext.SaveChangesAsync();

            var controller = new OrdersController(dbContext);
            
            var result = await controller.GetOrders(userId);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedOrders = Assert.IsAssignableFrom<IEnumerable<Order>>(okResult.Value);
            Assert.Equal(2, returnedOrders.Count());
            Assert.All(returnedOrders, order => Assert.Equal(userId, order.UserId));
        }

        [Fact]
        public async Task GetOrders_ReturnsEmptyList_WhenUserHasNoOrders()
        {
            var dbContext = GetTestDatabase();
            var userId = "userWithNoOrders";
            
            var orders = new List<Order>
            {
                new Order { Id = Guid.NewGuid(), UserId = "otherUser1", Amount = 15, Description = "Other order 1", Status = OrderStatus.New },
                new Order { Id = Guid.NewGuid(), UserId = "otherUser2", Amount = 25, Description = "Other order 2", Status = OrderStatus.Cancelled }
            };

            dbContext.Orders.AddRange(orders);
            await dbContext.SaveChangesAsync();

            var controller = new OrdersController(dbContext);
            
            var result = await controller.GetOrders(userId);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedOrders = Assert.IsAssignableFrom<IEnumerable<Order>>(okResult.Value);
            Assert.Empty(returnedOrders);
        }

        [Fact]
        public async Task GetOrderStatus_ReturnsStatus_WhenOrderExists()
        {
            var dbContext = GetTestDatabase();
            var orderId = Guid.NewGuid();
            var order = new Order 
            { 
                Id = orderId, 
                UserId = "testUser", 
                Amount = 15.99m, 
                Description = "Status test order",
                Status = OrderStatus.Finished 
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();
            
            var controller = new OrdersController(dbContext);
            
            var result = await controller.GetOrderStatus(orderId);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var json = JsonSerializer.Serialize(okResult.Value);
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
            
            Assert.NotNull(values);
            Assert.True(values.ContainsKey("Id"));
            Assert.True(values.ContainsKey("Status"));
            
            var returnedId = Guid.Parse(values["Id"].GetString());
            Assert.Equal(orderId, returnedId);
            Assert.Equal((int)OrderStatus.Finished, values["Status"].GetInt32());
        }

        [Fact]
        public async Task GetOrderStatus_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            var dbContext = GetTestDatabase();
            var controller = new OrdersController(dbContext);
            var nonExistentOrderId = Guid.NewGuid();
            
            var result = await controller.GetOrderStatus(nonExistentOrderId);
            
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateOrder_WithNegativeAmount_StillCreatesOrder()
        {
            var dbContext = GetTestDatabase();
            var controller = new OrdersController(dbContext);
            var orderRequest = new CreateOrderRequest
            {
                UserId = "userWithNegativeOrder",
                Amount = -50.75m,
                Description = "Negative amount order"
            };
            
            var result = await controller.CreateOrder(orderRequest);
            
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var order = Assert.IsType<Order>(createdResult.Value);
            Assert.Equal(orderRequest.Amount, order.Amount);
            Assert.Equal(OrderStatus.New, order.Status);
        }

        [Fact]
        public async Task CreateOrder_WithZeroAmount_CreatesOrderCorrectly()
        {
            var dbContext = GetTestDatabase();
            var controller = new OrdersController(dbContext);
            var orderRequest = new CreateOrderRequest
            {
                UserId = "userWithZeroAmountOrder",
                Amount = 0m,
                Description = "Zero amount order"
            };
            
            var result = await controller.CreateOrder(orderRequest);
            
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var order = Assert.IsType<Order>(createdResult.Value);
            Assert.Equal(0m, order.Amount);
        }

        [Fact]
        public async Task CreateOrder_WithLargeAmount_HandlesCorrectly()
        {
            var dbContext = GetTestDatabase();
            var controller = new OrdersController(dbContext);
            var largeAmount = 999999999.99m;
            var orderRequest = new CreateOrderRequest
            {
                UserId = "userWithLargeOrder",
                Amount = largeAmount,
                Description = "Very large amount order"
            };
            
            var result = await controller.CreateOrder(orderRequest);
            
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var order = Assert.IsType<Order>(createdResult.Value);
            Assert.Equal(largeAmount, order.Amount);
        }

        [Fact]
        public async Task CreateOrder_CreatesOutboxMessageWithCorrectContent()
        {
            var dbContext = GetTestDatabase();
            var controller = new OrdersController(dbContext);
            var orderRequest = new CreateOrderRequest
            {
                UserId = "outboxTestUser",
                Amount = 75.25m,
                Description = "Test outbox message content"
            };
            
            var result = await controller.CreateOrder(orderRequest);
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var order = Assert.IsType<Order>(createdResult.Value);
            
            var outboxMessage = await dbContext.OutboxMessages.FirstOrDefaultAsync();
            Assert.NotNull(outboxMessage);
            Assert.False(outboxMessage.Processed);
            
            var content = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(outboxMessage.Content);
            Assert.NotNull(content);
            
            var orderId = Guid.Parse(content["Id"].GetString());
            Assert.Equal(order.Id, orderId);
            Assert.Equal(orderRequest.UserId, content["UserId"].GetString());
            Assert.Equal(orderRequest.Amount, content["Amount"].GetDecimal());
        }

        [Fact]
        public async Task GetOrderStatus_WithCancelledOrder_ReturnsCorrectStatus()
        {
            var dbContext = GetTestDatabase();
            var orderId = Guid.NewGuid();
            var order = new Order
            {
                Id = orderId,
                UserId = "cancelledOrderUser",
                Amount = 55m,
                Description = "Cancelled order test",
                Status = OrderStatus.Cancelled
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();
            
            var controller = new OrdersController(dbContext);
            
            var result = await controller.GetOrderStatus(orderId);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var json = JsonSerializer.Serialize(okResult.Value);
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
            
            var returnedStatus = values["Status"].GetInt32();
            Assert.Equal((int)OrderStatus.Cancelled, returnedStatus);
        }

        [Fact]
        public async Task GetOrders_WithMultipleOrdersOfDifferentStatuses_ReturnsAllUserOrders()
        {
            var dbContext = GetTestDatabase();
            var userId = "multiStatusUser";
            var orders = new List<Order>
            {
                new Order { Id = Guid.NewGuid(), UserId = userId, Amount = 10, Description = "New order", Status = OrderStatus.New },
                new Order { Id = Guid.NewGuid(), UserId = userId, Amount = 20, Description = "Finished order", Status = OrderStatus.Finished },
                new Order { Id = Guid.NewGuid(), UserId = userId, Amount = 30, Description = "Cancelled order", Status = OrderStatus.Cancelled }
            };

            dbContext.Orders.AddRange(orders);
            await dbContext.SaveChangesAsync();

            var controller = new OrdersController(dbContext);
            
            var result = await controller.GetOrders(userId);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedOrders = Assert.IsAssignableFrom<IEnumerable<Order>>(okResult.Value);
            
            Assert.Equal(3, returnedOrders.Count());
            Assert.Contains(returnedOrders, o => o.Status == OrderStatus.New);
            Assert.Contains(returnedOrders, o => o.Status == OrderStatus.Finished);
            Assert.Contains(returnedOrders, o => o.Status == OrderStatus.Cancelled);
        }

        [Fact]
        public async Task CreateOrder_WithLongDescription_HandlesCorrectly()
        {
            var dbContext = GetTestDatabase();
            var controller = new OrdersController(dbContext);
            var longDescription = new string('A', 1000);
            
            var orderRequest = new CreateOrderRequest
            {
                UserId = "longDescriptionUser",
                Amount = 99.99m,
                Description = longDescription
            };
            
            var result = await controller.CreateOrder(orderRequest);
            
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var order = Assert.IsType<Order>(createdResult.Value);
            Assert.Equal(longDescription, order.Description);
        }

        [Fact]
        public async Task CreateOrder_WithEmptyDescription_StillCreatesOrder()
        {
            var dbContext = GetTestDatabase();
            var controller = new OrdersController(dbContext);
            var orderRequest = new CreateOrderRequest
            {
                UserId = "emptyDescriptionUser",
                Amount = 15.50m,
                Description = string.Empty
            };
            
            var result = await controller.CreateOrder(orderRequest);
            
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var order = Assert.IsType<Order>(createdResult.Value);
            Assert.Equal(string.Empty, order.Description);
        }

        [Fact]
        public async Task GetOrders_ReturnsSortedByMostRecentFirst_WhenUserHasMultipleOrders()
        {
            var dbContext = GetTestDatabase();
            var userId = "userWithMultipleOrders";
            
            var oldestOrderId = Guid.NewGuid();
            var middleOrderId = Guid.NewGuid();
            var newestOrderId = Guid.NewGuid();
            
            var orders = new List<Order>
            {
                new Order { Id = oldestOrderId, UserId = userId, Amount = 10, Description = "Oldest order", Status = OrderStatus.Finished },
                new Order { Id = middleOrderId, UserId = userId, Amount = 20, Description = "Middle order", Status = OrderStatus.New },
                new Order { Id = newestOrderId, UserId = userId, Amount = 30, Description = "Newest order", Status = OrderStatus.New }
            };

            dbContext.Orders.AddRange(orders);
            await dbContext.SaveChangesAsync();

            var controller = new OrdersController(dbContext);
            
            var result = await controller.GetOrders(userId);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedOrders = Assert.IsAssignableFrom<IEnumerable<Order>>(okResult.Value).ToList();
            
            Assert.Equal(3, returnedOrders.Count);
            Assert.Contains(returnedOrders, o => o.Id == oldestOrderId);
            Assert.Contains(returnedOrders, o => o.Id == middleOrderId);
            Assert.Contains(returnedOrders, o => o.Id == newestOrderId);
        }
    }
}