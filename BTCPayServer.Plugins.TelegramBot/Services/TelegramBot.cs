using BTCPayServer.Client.Models;
using BTCPayServer.Plugins.TelegramBot.Models;
using BTCPayServer.Services.Apps;
using Fido2NetLib;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BTCPayServer.Plugins.TelegramBot.Services
{
    public class TelegramBot(TelegramBotAppData appData,
        TelegramBotPluginService pluginService, ILogger<TelegramBot> logger)
    {

        public TelegramBotAppData AppData => appData;

        private ITelegramBotClient? _bot;
        private CancellationTokenSource? _cancellationTokenSource;

        private readonly Dictionary<long, UserSession> _userSessions = new();

        public void StartBot(CancellationToken cancellationToken)
        {
            _bot = new TelegramBotClient(appData.BotToken);

            var receiverOptions = new Telegram.Bot.Polling.ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
            };

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _bot.StartReceiving(
                HandleUpdate,
                HandleError,
                receiverOptions,
                _cancellationTokenSource.Token
            );

            var commands = new List<BotCommand>
            {
                new() { Command = "menu", Description = "🛍️ Browse Products" },
                new() { Command = "cart", Description = "🛒 My Cart" },
                new() { Command = "checkout", Description = "💳 Pay" },
                new() { Command = "clear", Description = "🗑️ Clear Cart" },
                new() { Command = "help", Description = "❓ Help" }
            };

            _bot.SetMyCommands(commands, cancellationToken: _cancellationTokenSource.Token)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        logger.LogError(t.Exception, "Error setting Telegram bot commands for app {AppName}", appData.Name);
                    }
                }, _cancellationTokenSource.Token);

            logger.LogInformation("Telegram bot started for app {AppName}", appData.Name);
        }

        public void StopBot()
        {
            try
            {
                // Annuler le token pour arrêter le polling du bot
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                _bot = null;

                logger.LogInformation("Telegram bot stopped for app {AppName}", appData.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error stopping Telegram bot for app {AppName}", appData.Name);
            }
        }

        private async Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken token)
        {
            try
            {
                if (update.Type == UpdateType.Message && update.Message?.Text != null)
                {
                    await HandleMessage(bot, update.Message, token);
                }
                else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                {
                    await HandleCallbackQuery(bot, update.CallbackQuery, token);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TelegramBot:HandleUpdate()");
            }
        }

        private async Task HandleMessage(ITelegramBotClient bot, Message message, CancellationToken token)
        {
            try
            {
                var chatId = message.Chat.Id;
                var text = message.Text!;
                var session = GetOrCreateSession(chatId, message.From);

                switch (text.ToLower())
                {
                    case "/start":
                        await SendWelcomeMessage(bot, chatId, token);
                        break;
                    case "/menu" or "/products":
                        await SendProductList(bot, chatId, token);
                        break;
                    case "/cart" or "/shopping":
                        await SendCartSummary(bot, chatId, token);
                        break;
                    case "/checkout" or "/pay":
                        await StartCheckout(bot, chatId, session, token);
                        break;
                    case "/clear" or "/empty":
                        session.Cart.Clear();
                        await bot.SendMessage(chatId, "🗑️ Your cart has been cleared.", cancellationToken: token);
                        break;
                    case "/help":
                        await SendHelpMessage(bot, chatId, token);
                        break;
                    default:
                        await HandleUserInput(bot, chatId, text, session, token);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TelegramBot:HandleMessage()");
            }
        }

        private async Task HandleUserInput(ITelegramBotClient bot, long chatId, string text, UserSession session, CancellationToken token)
        {
            try
            {
                switch (session.State)
                {
                    case UserState.WaitingForEmail:
                        if (IsValidEmail(text))
                        {
                            session.CustomerEmail = text;
                            session.State = session.RequiresShippingAddress ? UserState.WaitingForName : UserState.Ready;
                            if (session.RequiresShippingAddress)
                            {
                                await bot.SendMessage(chatId, "👤 Please enter your full name :", cancellationToken: token);
                            }
                            else
                            {
                                await FinalizeCheckout(bot, chatId, session, token);
                            }
                        }
                        else
                        {
                            await bot.SendMessage(chatId, "❌ Invalid email. Please enter a valid email address:", cancellationToken: token);
                        }
                        break;
                    case UserState.WaitingForName:
                        session.CustomerName = text;
                        session.State = UserState.WaitingForAddress;
                        await bot.SendMessage(chatId, "🏠 Please enter your street address:", cancellationToken: token);
                        break;
                    case UserState.WaitingForAddress:
                        session.CustomerAddress = text;
                        session.State = UserState.WaitingForCity;
                        await bot.SendMessage(chatId, "🏙️ Please enter your city:", cancellationToken: token);
                        break;
                    case UserState.WaitingForCity:
                        session.CustomerCity = text;
                        session.State = UserState.WaitingForPostalCode;
                        await bot.SendMessage(chatId, "📮 Please enter your postal code:", cancellationToken: token);
                        break;
                    case UserState.WaitingForPostalCode:
                        session.CustomerPostalCode = text;
                        session.State = UserState.WaitingForCountry;
                        await bot.SendMessage(chatId, "🌍 Please enter your country:", cancellationToken: token);
                        break;
                    case UserState.WaitingForCountry:
                        session.CustomerCountry = text;
                        session.State = UserState.Ready;
                        await FinalizeCheckout(bot, chatId, session, token);
                        break;
                    case UserState.WaitingForQuantity:
                        if (int.TryParse(text, out var quantity) && quantity > 0 && session.PendingItemId != null)
                        {
                            await AddItemToCart(bot, chatId, session.PendingItemId, quantity, session, token);
                            session.PendingItemId = null;
                            session.State = UserState.Browsing;
                        }
                        else
                        {
                            await bot.SendMessage(chatId, "❌ Invalid quantity. Please enter a positive number:", cancellationToken: token);
                        }
                        break;
                    default:
                        await bot.SendMessage(chatId, "💡 Use /menu to view products or /help for assistance.", cancellationToken: token);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TelegramBot:HandleUserInput()");
            }
        }

        private async Task HandleCallbackQuery(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken token)
        {
            try
            {
                var chatId = callbackQuery.Message!.Chat.Id;
                var data = callbackQuery.Data!;
                var session = GetOrCreateSession(chatId, callbackQuery.From);

                await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: token);

                if (data.StartsWith("view_"))
                {
                    var itemId = data[5..];
                    await SendProductDetails(bot, chatId, itemId, token);
                }
                else if (data.StartsWith("add_"))
                {
                    var itemId = data[4..];
                    session.PendingItemId = itemId;
                    session.State = UserState.WaitingForQuantity;
                    await bot.SendMessage(chatId, "🔢 Enter the desired quantity:", cancellationToken: token);
                }
                else if (data.StartsWith("quick_add_"))
                {
                    var itemId = data[10..];
                    await AddItemToCart(bot, chatId, itemId, 1, session, token);
                }
                else if (data.StartsWith("remove_"))
                {
                    var itemId = data[7..];
                    session.Cart.Remove(itemId);
                    await SendCartSummary(bot, chatId, token);
                }
                else if (data.StartsWith("cat_"))
                {
                    var category = data[4..];
                    await SendProductsByCategory(bot, chatId, category, token);
                }
                else if (data == "checkout")
                {
                    await StartCheckout(bot, chatId, session, token);
                }
                else if (data == "continue")
                {
                    await SendProductList(bot, chatId, token);
                }
                else if (data == "cart")
                {
                    await SendCartSummary(bot, chatId, token);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TelegramBot:HandleCallbackQuery()");
            }
        }
        private async Task SendWelcomeMessage(ITelegramBotClient bot, long chatId, CancellationToken token)
        {
            try
            {
                if (appData == null) return;

                var welcomeText = $"🎉 Welcome to *{EscapeMarkdown(appData.Title ?? appData.Name)}* !\n\n";

                if (!string.IsNullOrEmpty(appData.Description))
                {
                    welcomeText += $"{EscapeMarkdown(appData.Description)}\n\n";
                }

                welcomeText += "📦 Use the commands below to navigate:";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("🛍️ Browse Products", "cat_all") },
                new[] { InlineKeyboardButton.WithCallbackData("🛒 My Cart", "cart") }
            });

                await bot.SendMessage(chatId, welcomeText, parseMode: ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TelegramBot:SendWelcomeMessage()");
            }
        }

        private async Task SendProductList(ITelegramBotClient bot, long chatId, CancellationToken token)
        {
            await SendProductsByCategory(bot, chatId, "all", token);
        }

        private async Task SendProductsByCategory(ITelegramBotClient bot, long chatId, string category, CancellationToken token)
        {
            try
            {
                if (appData?.ShopItems == null || !appData.ShopItems.Any())
                {
                    await bot.SendMessage(chatId, "😢 No products in this category.", cancellationToken: token);
                    return;
                }

                var items = appData.ShopItems
                    .Where(i => !i.Disabled)
                    .Where(i => category == "all" || (i.Categories?.Contains(category) ?? false))
                    .ToList();

                if (!items.Any())
                {
                    await bot.SendMessage(chatId, "😢 No products in this category.", cancellationToken: token);
                    return;
                }

                var categories = appData.ShopItems
                    .Where(i => !i.Disabled && i.Categories != null)
                    .SelectMany(i => i.Categories!)
                    .Distinct()
                    .ToList();

                if (categories.Any())
                {
                    var catButtons = categories.Select(c =>
                        InlineKeyboardButton.WithCallbackData($"📂 {c}", $"cat_{c}")).ToList();
                    catButtons.Insert(0, InlineKeyboardButton.WithCallbackData("📂 All", "cat_all"));

                    var catKeyboard = new InlineKeyboardMarkup(catButtons.Chunk(2).Select(chunk => chunk.ToArray()));
                    await bot.SendMessage(chatId, "📁 *Available Categories:*", parseMode: ParseMode.Markdown, replyMarkup: catKeyboard, cancellationToken: token);
                }

                foreach (var item in items.Take(10)) // Limiter à 10 pour éviter le spam
                {
                    await SendProductCard(bot, chatId, item, token);
                }

                if (items.Count > 10)
                {
                    await bot.SendMessage(chatId, $"... and {items.Count - 10} more products. Use categories to filter.", cancellationToken: token);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TelegramBot:SendProductsByCategory()");
            }
        }

        private async Task SendProductCard(ITelegramBotClient bot, long chatId, AppItem item, CancellationToken token)
        {
            try
            {
                var priceText = item.PriceType switch
                {
                    AppItemPriceType.Fixed => $"💰 {item.Price} {appData?.CurrencyCode}",
                    AppItemPriceType.Minimum => $"💰 Minimum {item.Price} {appData?.CurrencyCode}",
                    AppItemPriceType.Topup => "💰 Pay what you want",
                    _ => ""
                };

                var stockText = "";
                if (item.Inventory.HasValue)
                {
                    stockText = item.Inventory > 0
                        ? $"\n📦 Stock: {item.Inventory} available"
                        : "\n❌ Out of stock";
                }

                var taxText = item.TaxRate.HasValue && item.TaxRate > 0
                    ? $"\n🧾 Tax: {item.TaxRate}%"
                    : "";

                var caption = $"*{EscapeMarkdown(item.Title)}*\n\n{EscapeMarkdown(item.Description ?? "")}\n\n{priceText}{stockText}{taxText}";

                var buttons = new List<InlineKeyboardButton[]>();

                if (!item.Inventory.HasValue || item.Inventory > 0)
                {
                    buttons.Add([
                        InlineKeyboardButton.WithCallbackData("➕ Ajouter (1)", $"quick_add_{item.Id}"),
                    InlineKeyboardButton.WithCallbackData("🔢 Quantité", $"add_{item.Id}")
                    ]);
                }

                var keyboard = new InlineKeyboardMarkup(buttons);

                var imageUrl = item.Image;

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    if (imageUrl.StartsWith("~/", StringComparison.Ordinal))
                    {
                        imageUrl = imageUrl.Substring(1);
                    }

                    if (!imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        imageUrl = pluginService.GetConfig().BaseUrl.TrimEnd('/') + imageUrl;
                    }
                    if (!imageUrl.ToLower().Contains("localhost"))
                    {
                        await bot.SendPhoto(chatId, InputFile.FromUri(imageUrl), caption: caption, parseMode: ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: token);
                        return;
                    }
                }

                await bot.SendMessage(chatId, caption, parseMode: ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TelegramBot:SendProductCard()");
            }
        }

        private async Task SendProductDetails(ITelegramBotClient bot, long chatId, string itemId, CancellationToken token)
        {
            try
            {
                var item = appData?.ShopItems?.FirstOrDefault(i => i.Id == itemId);
                if (item == null)
                {
                    await bot.SendMessage(chatId, "❌ Product not found.", cancellationToken: token);
                    return;
                }

                await SendProductCard(bot, chatId, item, token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TelegramBot:SendProductDetails()");
            }
        }

        private async Task AddItemToCart(ITelegramBotClient bot, long chatId, string itemId, int quantity, UserSession session, CancellationToken token)
        {
            try
            {
                var item = appData?.ShopItems?.FirstOrDefault(i => i.Id == itemId);
                if (item == null)
                {
                    await bot.SendMessage(chatId, "❌ Product not found.", cancellationToken: token);
                    return;
                }

                if (item.Inventory.HasValue)
                {
                    var currentInCart = session.Cart.GetValueOrDefault(itemId, 0);
                    if (currentInCart + quantity > item.Inventory.Value)
                    {
                        await bot.SendMessage(chatId, $"❌ Insufficient stock. Available: {item.Inventory - currentInCart}", cancellationToken: token);
                        return;
                    }
                }

                session.Cart[itemId] = session.Cart.GetValueOrDefault(itemId, 0) + quantity;

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🛒 View Cart", "cart"),
                    InlineKeyboardButton.WithCallbackData("🛍️ Continue Shopping", "continue")
                }
            });

                await bot.SendMessage(chatId, $"✅ *{quantity}x {EscapeMarkdown(item.Title)}* added to cart!",
                    parseMode: ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TelegramBot:AddItemToCart()");
            }
        }

        private async Task SendCartSummary(ITelegramBotClient bot, long chatId, CancellationToken token)
        {
            try
            {
                var session = GetOrCreateSession(chatId);

                if (!session.Cart.Any())
                {
                    var emptyKeyboard = new InlineKeyboardMarkup(new[]
                    {
                    new[] { InlineKeyboardButton.WithCallbackData("🛍️ Browse Products", "cat_all") }
                });
                    await bot.SendMessage(chatId, "🛒 Your cart is empty.", replyMarkup: emptyKeyboard, cancellationToken: token);
                    return;
                }

                var cartText = "🛒 *Your Cart:*\n\n";
                decimal total = 0;
                decimal totalTax = 0;
                var buttons = new List<InlineKeyboardButton[]>();

                foreach (var (itemId, quantity) in session.Cart)
                {
                    var item = appData?.ShopItems?.FirstOrDefault(i => i.Id == itemId);
                    if (item == null) continue;

                    var itemPrice = item.Price ?? 0;
                    var itemTotal = itemPrice * quantity;
                    var itemTax = item.TaxRate.HasValue ? itemTotal * (item.TaxRate.Value / 100) : 0;

                    total += itemTotal;
                    totalTax += itemTax;

                    cartText += $"• *{EscapeMarkdown(item.Title)}* x{quantity} = {itemTotal} {appData?.CurrencyCode}\n";
                    buttons.Add([InlineKeyboardButton.WithCallbackData($"❌ Remove {item.Title}", $"remove_{itemId}")]);
                }

                if (totalTax > 0)
                {
                    cartText += $"\n🧾 Tax: {totalTax:F2} {appData?.CurrencyCode}";
                    total += totalTax;
                }

                cartText += $"\n\n💳 *Total: {total:F2} {appData?.CurrencyCode}*";

                buttons.Add([
                    InlineKeyboardButton.WithCallbackData("💳 Checkout", "checkout"),
                InlineKeyboardButton.WithCallbackData("🛍️ Continue Shopping", "continue")
                ]);

                var keyboard = new InlineKeyboardMarkup(buttons);
                await bot.SendMessage(chatId, cartText, parseMode: ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TelegramBot:SendCartSummary()");
            }

        }

        private async Task StartCheckout(ITelegramBotClient bot, long chatId, UserSession session, CancellationToken token)
        {
            try
            {
                if (!session.Cart.Any())
                {
                    await bot.SendMessage(chatId, "🛒 Your cart is empty. Add products before proceeding to checkout.", cancellationToken: token);
                    return;
                }


                var requiresEmail = false;
                session.RequiresShippingAddress = false;

                switch (appData.FormId)
                {
                    case "":
                        break;
                    case "Email":
                        requiresEmail = true;
                        break;
                    default:
                        requiresEmail = true;
                        session.RequiresShippingAddress = true;
                        break;
                }

                if (requiresEmail && string.IsNullOrEmpty(session.CustomerEmail))
                {
                    session.State = UserState.WaitingForEmail;
                    await bot.SendMessage(chatId, "📧 Please enter your email address:", cancellationToken: token);
                    return;
                }

                await FinalizeCheckout(bot, chatId, session, token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TelegramBot:StartCheckout()");
            }
        }

        private async Task FinalizeCheckout(ITelegramBotClient bot, long chatId, UserSession session, CancellationToken token)
        {
            try
            {
                decimal total = 0;
                var cartItems = new List<PosCartItem>();

                foreach (var (itemId, quantity) in session.Cart)
                {
                    var item = appData.ShopItems?.FirstOrDefault(i => i.Id == itemId);
                    if (item == null) continue;

                    var itemPrice = item.Price ?? 0;
                    var itemTotal = itemPrice * quantity;
                    var itemTax = item.TaxRate.HasValue ? itemTotal * (item.TaxRate.Value / 100) : 0;

                    total += itemTotal + itemTax;

                    cartItems.Add(new PosCartItem
                    {
                        Id = itemId,
                        Title = item.Title,
                        Count = quantity,
                        Price = itemPrice
                    });
                }

                var invoiceUrl = await pluginService.CreateInvoiceAsync(
                    appData.StoreDataId,
                    appData.Id,
                    appData.Name,
                    total,
                    appData.CurrencyCode,
                    cartItems,
                    session.TelegramUser,
                    session.CustomerEmail,
                    session.CustomerName,
                    session.CustomerAddress,
                    session.CustomerCity,
                    session.CustomerPostalCode,
                    session.CustomerCountry,
                    chatId);

                if (string.IsNullOrEmpty(invoiceUrl))
                {
                    await bot.SendMessage(chatId, "❌ Error creating invoice. Please try again.", cancellationToken: token);
                    return;
                }

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithUrl("💳 Pay Now", invoiceUrl) }
                });

                await bot.SendMessage(chatId,
                    $"✅ *Invoice Created!*\n\n💰 Total: {total:F2} {appData.CurrencyCode}\n\nClick below to proceed to payment:",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard,
                    cancellationToken: token);

                session.Cart.Clear();
                session.State = UserState.Browsing;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TelegramBot:FinalizeCheckout()");
                await bot.SendMessage(chatId, "❌ Error creating invoice. Please try again.", cancellationToken: token);
            }
        }

        public async Task SendPaymentSuccess(long chatId)
        {
            if (_bot == null) return;
            await _bot.SendMessage(chatId, "✅ Payment successful! Thank you for your purchase.");
        }

        public async Task SendPaymentFailure(long chatId)
        {
            if (_bot == null) return;
            await _bot.SendMessage(chatId, "❌ Payment failed or was cancelled. Please try again.");
        }


        private async Task SendHelpMessage(ITelegramBotClient bot, long chatId, CancellationToken token)
        {
            var helpText = """
                📖 *Help - Available Commands:*

                /start - Welcome message
                /menu - Display products
                /cart - View your cart
                /checkout - Proceed to payment
                /clear - Empty your cart
                /help - Show this help

                💡 *How to Order:*
                1. Browse products with /menu
                2. Add items to your cart
                3. Review your cart with /cart
                4. Pay with /checkout

                🔒 Secure payment via Bitcoin/Lightning and altcoins
                """;

            await bot.SendMessage(chatId, helpText, parseMode: ParseMode.Markdown, cancellationToken: token);
        }

        private Task HandleError(ITelegramBotClient bot, Exception ex, CancellationToken token)
        {
            logger.LogError(ex, "Telegram bot error");
            return Task.CompletedTask;
        }

        private UserSession GetOrCreateSession(long chatId, User? telegramUser = null)
        {
            if (!_userSessions.TryGetValue(chatId, out var session))
            {
                session = new UserSession();
                if (telegramUser != null)
                {
                    var username = !string.IsNullOrEmpty(telegramUser.Username)
                        ? telegramUser.Username
                        : $"{telegramUser.FirstName} {telegramUser.LastName}".Trim();
                    session.TelegramUser = username;
                }
                _userSessions[chatId] = session;
            }
            return session;
        }
        private static string EscapeMarkdown(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text
                .Replace("_", "\\_")
                .Replace("*", "\\*")
                .Replace("[", "\\[")
                .Replace("`", "\\`");
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    public class UserSession
    {
        public Dictionary<string, int> Cart { get; set; } = new();
        public UserState State { get; set; } = UserState.Browsing;
        public string? PendingItemId { get; set; }
        public string? TelegramUser { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerCity { get; set; }
        public string? CustomerPostalCode { get; set; }
        public string? CustomerCountry { get; set; }
        public bool RequiresShippingAddress { get; set; }
    }

    public enum UserState
    {
        Browsing,
        WaitingForQuantity,
        WaitingForEmail,
        WaitingForName,
        WaitingForAddress,
        WaitingForCity,
        WaitingForPostalCode,
        WaitingForCountry,
        Ready
    }

    public class PosCartItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public int Count { get; set; }
        public decimal Price { get; set; }
    }
}
