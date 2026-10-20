using System;
using System.Collections.Generic;
using System.Linq;

namespace AuraEngine
{
    public enum OrderStatus { New, Preparing, Ready, Served }
    public enum PurchaseOrderStatus { Draft, SentToSupplier, Received }
    public enum TableStatus { Free, Reserved, Seated, BillRequested }

    public class SupplierCatalogQuote
    {
        public string SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string IngredientId { get; set; }
        public decimal UnitPrice { get; set; }
        public int LeadTimeHours { get; set; }
    }

    public class DishSentimentAudit
    {
        public string MenuItemId { get; set; }
        public string DishName { get; set; }
        public decimal ContributionMarginEur { get; set; }
        public decimal ReviewRatingOutOfFive { get; set; }
        public string DominantComplaintKeyword { get; set; }
        public bool IsMarginTrap
        {
            get { return ContributionMarginEur >= 15.0m && ReviewRatingOutOfFive < 3.8m; }
        }
    }

    public class RestaurantTable
    {
        public string TableCode { get; set; }
        public string ZoneName { get; set; }
        public int MaxSeats { get; set; }
        public int ActiveCovers { get; set; }
        public TableStatus Status { get; set; }
        public decimal RunningBill { get; set; }
    }

    public class HaccpFridgeReading
    {
        public string FridgeId { get; set; }
        public string Name { get; set; }
        public decimal TemperatureCelsius { get; set; }
        public decimal MaxAllowedCelsius { get; set; }
        public bool IsCompliant { get { return TemperatureCelsius <= MaxAllowedCelsius; } }
    }

    public class Ingredient
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal MinThreshold { get; set; }
        public decimal ParLevel { get; set; }
        public decimal UnitCost { get; set; }
        public string SupplierId { get; set; }
        public string AllergenTag { get; set; }
        public string ActiveLotCode { get; set; }

        public bool IsLowStock { get { return CurrentStock <= MinThreshold; } }
    }

    public class RecipeLine
    {
        public string IngredientId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class MenuItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string KitchenStation { get; set; }
        public decimal SellingPrice { get; set; }
        public List<RecipeLine> Recipe { get; set; }
        public int PopularityScore { get; set; }
        public decimal ReviewScore { get; set; }
        public string ReviewFeedbackNote { get; set; }
        public bool IsAvailable { get; set; }

        public MenuItem()
        {
            Recipe = new List<RecipeLine>();
            IsAvailable = true;
            KitchenStation = "Primi";
            ReviewScore = 4.8m;
            ReviewFeedbackNote = "Eccellente";
        }
    }

    public class KdsOrderTicket
    {
        public string TicketId { get; set; }
        public string TableCode { get; set; }
        public string Source { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<string> MenuItemIds { get; set; }
        public List<string> AllergenAlerts { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TheoreticalFoodCost { get; set; }

        public KdsOrderTicket()
        {
            MenuItemIds = new List<string>();
            AllergenAlerts = new List<string>();
        }
    }

    public class PurchaseOrderLine
    {
        public string IngredientId { get; set; }
        public string IngredientName { get; set; }
        public decimal OrderedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get { return Math.Round(OrderedQuantity * UnitPrice, 2); } }
    }

    public class PurchaseOrder
    {
        public string PoNumber { get; set; }
        public string SupplierId { get; set; }
        public PurchaseOrderStatus Status { get; set; }
        public List<PurchaseOrderLine> Lines { get; set; }
        public decimal TotalCost { get { return Lines.Sum(l => l.LineTotal); } }

        public PurchaseOrder()
        {
            Lines = new List<PurchaseOrderLine>();
        }
    }

    public class FinancialSnapshot
    {
        public decimal GrossRevenue { get; set; }
        public decimal TheoreticalFoodCost { get; set; }
        public decimal ActualWasteAndVariance { get; set; }
        public decimal TotalCogs { get { return TheoreticalFoodCost + ActualWasteAndVariance; } }
        public decimal LaborCost { get; set; }
        public decimal FixedOperatingExpenses { get; set; }
        public decimal TheoreticalFoodCostPercentage
        {
            get { return GrossRevenue > 0 ? Math.Round((TheoreticalFoodCost / GrossRevenue) * 100m, 2) : 0m; }
        }
        public decimal FoodCostPercentage
        {
            get { return GrossRevenue > 0 ? Math.Round((TotalCogs / GrossRevenue) * 100m, 2) : 0m; }
        }
        public decimal FoodCostVariancePercentage
        {
            get { return Math.Round(FoodCostPercentage - TheoreticalFoodCostPercentage, 2); }
        }
        public decimal LaborCostPercentage
        {
            get { return GrossRevenue > 0 ? Math.Round((LaborCost / GrossRevenue) * 100m, 2) : 0m; }
        }
        public decimal PrimeCostPercentage
        {
            get { return FoodCostPercentage + LaborCostPercentage; }
        }
        public decimal NetOperatingIncome
        {
            get { return Math.Round(GrossRevenue - TotalCogs - LaborCost - FixedOperatingExpenses, 2); }
        }
    }

    public class AuraRestaurantEngine
    {
        private readonly Dictionary<string, Ingredient> _inventory = new Dictionary<string, Ingredient>();
        private readonly Dictionary<string, MenuItem> _menu = new Dictionary<string, MenuItem>();
        private readonly Dictionary<string, RestaurantTable> _tables = new Dictionary<string, RestaurantTable>();
        private readonly List<HaccpFridgeReading> _fridges = new List<HaccpFridgeReading>();
        private readonly List<KdsOrderTicket> _kdsQueue = new List<KdsOrderTicket>();
        private readonly List<PurchaseOrder> _purchaseOrders = new List<PurchaseOrder>();
        private readonly List<string> _priceVarianceAlerts = new List<string>();
        private readonly List<SupplierCatalogQuote> _supplierQuotes = new List<SupplierCatalogQuote>();

        public decimal DailyLaborCost { get; set; }
        public decimal DailyFixedOpEx { get; set; }
        public decimal RecordedWasteCost { get; set; }

        public AuraRestaurantEngine()
        {
            DailyLaborCost = 850.00m;
            DailyFixedOpEx = 420.00m;
            RecordedWasteCost = 0.00m;
        }

        public IDictionary<string, Ingredient> Inventory { get { return _inventory; } }
        public IDictionary<string, MenuItem> Menu { get { return _menu; } }
        public IDictionary<string, RestaurantTable> Tables { get { return _tables; } }
        public IList<HaccpFridgeReading> Fridges { get { return _fridges; } }
        public IList<KdsOrderTicket> KdsQueue { get { return _kdsQueue; } }
        public IList<PurchaseOrder> PurchaseOrders { get { return _purchaseOrders; } }
        public IList<string> PriceVarianceAlerts { get { return _priceVarianceAlerts; } }

        public void RegisterSupplierQuote(SupplierCatalogQuote quote)
        {
            if (quote == null) throw new ArgumentException("Invalid quote.");
            _supplierQuotes.Add(quote);
        }

        /// <summary>
        /// Market Gap #2: AI Multi-Vendor Price Broker + Weather/Booking Demand Factor.
        /// Finds the lowest cost vendor for an ingredient and scales restock by forecast multiplier.
        /// </summary>
        public PurchaseOrder GenerateSmartBrokerPurchaseOrder(string ingredientId, decimal weatherAndBookingDemandMultiplier)
        {
            Ingredient ing;
            if (!_inventory.TryGetValue(ingredientId, out ing))
                throw new KeyNotFoundException("Ingredient not found.");

            var matchingQuotes = _supplierQuotes.Where(q => q.IngredientId == ingredientId).OrderBy(q => q.UnitPrice).ToList();
            string chosenSupplier = ing.SupplierId;
            decimal bestPrice = ing.UnitCost;

            if (matchingQuotes.Count > 0)
            {
                chosenSupplier = matchingQuotes[0].SupplierId;
                bestPrice = matchingQuotes[0].UnitPrice;
            }

            decimal baseNeeded = Math.Max(0m, ing.ParLevel - ing.CurrentStock);
            decimal adjustedQuantity = Math.Round(baseNeeded * weatherAndBookingDemandMultiplier, 2);

            var po = new PurchaseOrder
            {
                PoNumber = string.Format("PO-AI-{0:D4}", _purchaseOrders.Count + 1),
                SupplierId = chosenSupplier,
                Status = PurchaseOrderStatus.SentToSupplier
            };

            po.Lines.Add(new PurchaseOrderLine
            {
                IngredientId = ing.Id,
                IngredientName = ing.Name,
                OrderedQuantity = adjustedQuantity,
                UnitPrice = bestPrice
            });

            _purchaseOrders.Add(po);
            return po;
        }

        /// <summary>
        /// Market Gap #1: Sentiment-to-Margin Matrix.
        /// Identifies dishes that have high financial margin but poor customer review sentiment ("Margin Traps").
        /// </summary>
        public List<DishSentimentAudit> AnalyzeSentimentToMarginMatrix()
        {
            var result = new List<DishSentimentAudit>();
            foreach (var item in _menu.Values)
            {
                decimal cost = ComputeRecipeCost(item.Id);
                decimal margin = Math.Round(item.SellingPrice - cost, 2);
                result.Add(new DishSentimentAudit
                {
                    MenuItemId = item.Id,
                    DishName = item.Name,
                    ContributionMarginEur = margin,
                    ReviewRatingOutOfFive = item.ReviewScore,
                    DominantComplaintKeyword = item.ReviewFeedbackNote
                });
            }
            return result;
        }

        /// <summary>
        /// Market Gap #3: Zero-Effort Invoice UoM Converter (Cases -> Packs -> Base Recipe Unit).
        /// </summary>
        public decimal ReceiveCaseInvoiceWithAutoUoMConversion(
            string ingredientId,
            int casesDelivered,
            int packsPerCase,
            decimal baseUnitsPerPack,
            decimal totalInvoiceLineEur)
        {
            Ingredient ing;
            if (!_inventory.TryGetValue(ingredientId, out ing))
                throw new KeyNotFoundException("Ingredient not found.");

            decimal totalBaseUnitsAdded = Math.Round(casesDelivered * packsPerCase * baseUnitsPerPack, 4);
            if (totalBaseUnitsAdded <= 0)
                throw new ArgumentException("Invalid UoM conversion quantities.");

            ing.CurrentStock = Math.Round(ing.CurrentStock + totalBaseUnitsAdded, 4);
            ing.UnitCost = Math.Round(totalInvoiceLineEur / totalBaseUnitsAdded, 4);
            RefreshMenuAvailability();
            return totalBaseUnitsAdded;
        }

        public void RegisterTable(RestaurantTable table)
        {
            if (table == null || string.IsNullOrWhiteSpace(table.TableCode))
                throw new ArgumentException("Invalid table.");
            _tables[table.TableCode] = table;
        }

        public void CheckInTable(string tableCode, int covers)
        {
            RestaurantTable table;
            if (!_tables.TryGetValue(tableCode, out table))
                throw new KeyNotFoundException(string.Format("Table {0} not found.", tableCode));
            table.ActiveCovers = covers;
            table.Status = TableStatus.Seated;
        }

        public void RegisterFridgeReading(HaccpFridgeReading reading)
        {
            if (reading == null) throw new ArgumentException("Invalid reading.");
            _fridges.Add(reading);
        }

        public void RecordKitchenWaste(string ingredientId, decimal wastedQuantity)
        {
            Ingredient ing;
            if (!_inventory.TryGetValue(ingredientId, out ing))
                throw new KeyNotFoundException(string.Format("Ingredient {0} not found.", ingredientId));
            ing.CurrentStock = Math.Max(0m, Math.Round(ing.CurrentStock - wastedQuantity, 4));
            RecordedWasteCost = Math.Round(RecordedWasteCost + (wastedQuantity * ing.UnitCost), 2);
            RefreshMenuAvailability();
        }

        public void RegisterIngredient(Ingredient ingredient)
        {
            if (ingredient == null || string.IsNullOrWhiteSpace(ingredient.Id))
                throw new ArgumentException("Invalid ingredient.");
            _inventory[ingredient.Id] = ingredient;
            RefreshMenuAvailability();
        }

        public void RegisterMenuItem(MenuItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id))
                throw new ArgumentException("Invalid menu item.");
            _menu[item.Id] = item;
            RefreshMenuAvailability();
        }

        public decimal ComputeRecipeCost(string menuItemId)
        {
            MenuItem item;
            if (!_menu.TryGetValue(menuItemId, out item))
                throw new KeyNotFoundException(string.Format("Menu item {0} not found.", menuItemId));

            decimal cost = 0m;
            foreach (var line in item.Recipe)
            {
                Ingredient ing;
                if (_inventory.TryGetValue(line.IngredientId, out ing))
                {
                    cost += line.Quantity * ing.UnitCost;
                }
            }
            return Math.Round(cost, 2);
        }

        public List<string> GetMenuItemAllergens(string menuItemId)
        {
            MenuItem item;
            if (!_menu.TryGetValue(menuItemId, out item))
                return new List<string>();

            return item.Recipe
                .Select(r => _inventory.ContainsKey(r.IngredientId) ? _inventory[r.IngredientId].AllergenTag : null)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct()
                .ToList();
        }

        public void RefreshMenuAvailability()
        {
            foreach (var item in _menu.Values)
            {
                bool canPrepare = true;
                foreach (var line in item.Recipe)
                {
                    Ingredient ing;
                    if (!_inventory.TryGetValue(line.IngredientId, out ing) || ing.CurrentStock < line.Quantity)
                    {
                        canPrepare = false;
                        break;
                    }
                }
                item.IsAvailable = canPrepare;
            }
        }

        public KdsOrderTicket SubmitQrOrder(string tableCode, List<string> menuItemIds)
        {
            if (string.IsNullOrWhiteSpace(tableCode))
                throw new ArgumentException("Table code is required.");
            if (menuItemIds == null || menuItemIds.Count == 0)
                throw new ArgumentException("Order must contain at least one item.");

            RefreshMenuAvailability();

            foreach (var id in menuItemIds)
            {
                MenuItem dish;
                if (!_menu.TryGetValue(id, out dish))
                    throw new InvalidOperationException(string.Format("Dish {0} does not exist.", id));
                if (!dish.IsAvailable)
                    throw new InvalidOperationException(string.Format("Dish {0} is currently 86'd (out of stock).", dish.Name));
            }

            decimal totalRevenue = 0m;
            decimal totalFoodCost = 0m;
            var allergens = new HashSet<string>();

            foreach (var id in menuItemIds)
            {
                var dish = _menu[id];
                totalRevenue += dish.SellingPrice;
                totalFoodCost += ComputeRecipeCost(id);

                foreach (var line in dish.Recipe)
                {
                    var ing = _inventory[line.IngredientId];
                    ing.CurrentStock = Math.Round(ing.CurrentStock - line.Quantity, 4);
                    if (!string.IsNullOrWhiteSpace(ing.AllergenTag))
                        allergens.Add(ing.AllergenTag);
                }
            }

            RefreshMenuAvailability();

            RestaurantTable table;
            if (_tables.TryGetValue(tableCode, out table))
            {
                table.Status = TableStatus.Seated;
                table.RunningBill = Math.Round(table.RunningBill + totalRevenue, 2);
            }

            var ticket = new KdsOrderTicket
            {
                TicketId = string.Format("KDS-{0:D4}", _kdsQueue.Count + 1),
                TableCode = tableCode,
                Source = "QR_TABLE",
                Status = OrderStatus.New,
                CreatedAtUtc = DateTime.UtcNow,
                MenuItemIds = new List<string>(menuItemIds),
                AllergenAlerts = allergens.ToList(),
                TotalAmount = Math.Round(totalRevenue, 2),
                TheoreticalFoodCost = Math.Round(totalFoodCost, 2)
            };

            _kdsQueue.Add(ticket);
            return ticket;
        }

        public List<PurchaseOrder> GenerateAutomatedPurchaseOrders()
        {
            var lowStockItems = _inventory.Values.Where(i => i.IsLowStock).ToList();
            var groupedBySupplier = lowStockItems.GroupBy(i => i.SupplierId);
            var createdOrders = new List<PurchaseOrder>();

            foreach (var group in groupedBySupplier)
            {
                var po = new PurchaseOrder
                {
                    PoNumber = string.Format("PO-{0:D4}", _purchaseOrders.Count + 1),
                    SupplierId = group.Key,
                    Status = PurchaseOrderStatus.SentToSupplier
                };

                foreach (var ing in group)
                {
                    decimal needed = Math.Round(ing.ParLevel - ing.CurrentStock, 2);
                    if (needed > 0)
                    {
                        po.Lines.Add(new PurchaseOrderLine
                        {
                            IngredientId = ing.Id,
                            IngredientName = ing.Name,
                            OrderedQuantity = needed,
                            UnitPrice = ing.UnitCost
                        });
                    }
                }

                if (po.Lines.Count > 0)
                {
                    _purchaseOrders.Add(po);
                    createdOrders.Add(po);
                }
            }

            return createdOrders;
        }

        public void ReceiveSupplierDelivery(string poNumber, Dictionary<string, decimal> billedUnitPrices, string newHaccpLotCode)
        {
            var po = _purchaseOrders.FirstOrDefault(p => p.PoNumber == poNumber);
            if (po == null)
                throw new KeyNotFoundException(string.Format("Purchase Order {0} not found.", poNumber));

            foreach (var line in po.Lines)
            {
                var ing = _inventory[line.IngredientId];
                ing.CurrentStock = Math.Round(ing.CurrentStock + line.OrderedQuantity, 4);
                ing.ActiveLotCode = newHaccpLotCode;

                decimal newPrice;
                if (billedUnitPrices != null && billedUnitPrices.TryGetValue(ing.Id, out newPrice))
                {
                    decimal previousCost = ing.UnitCost;
                    if (previousCost > 0)
                    {
                        decimal variancePct = Math.Round(((newPrice - previousCost) / previousCost) * 100m, 2);
                        if (variancePct > 5.0m)
                        {
                            _priceVarianceAlerts.Add(
                                string.Format("ALERT [{0}]: Supplier {1} increased price by +{2}% ({3:F2} -> {4:F2} EUR/{5})",
                                    ing.Name, po.SupplierId, variancePct, previousCost, newPrice, ing.Unit));
                        }
                    }
                    ing.UnitCost = newPrice;
                }
            }

            po.Status = PurchaseOrderStatus.Received;
            RefreshMenuAvailability();
        }

        public FinancialSnapshot GetLiveFinancialSnapshot()
        {
            decimal grossRev = _kdsQueue.Sum(t => t.TotalAmount);
            decimal theoCogs = _kdsQueue.Sum(t => t.TheoreticalFoodCost);

            return new FinancialSnapshot
            {
                GrossRevenue = Math.Round(grossRev, 2),
                TheoreticalFoodCost = Math.Round(theoCogs, 2),
                ActualWasteAndVariance = Math.Round(RecordedWasteCost, 2),
                LaborCost = Math.Round(DailyLaborCost, 2),
                FixedOperatingExpenses = Math.Round(DailyFixedOpEx, 2)
            };
        }
    }
}
