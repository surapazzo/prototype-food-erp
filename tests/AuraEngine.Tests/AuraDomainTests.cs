using System;
using System.Collections.Generic;
using System.Linq;
using AuraEngine;

namespace AuraEngine.Tests
{
    public class TestCaseEntry
    {
        public string Name { get; set; }
        public Action Test { get; set; }
    }

    public static class AuraTestRunner
    {
        public static int RunAllTests()
        {
            int passed = 0;
            int failed = 0;

            var tests = new List<TestCaseEntry>
            {
                new TestCaseEntry { Name = "TC-01: QR Order creates KDS Ticket, tracks EU-1169/2011 Allergens & deducts exact BoM grams", Test = Test_QrOrderAndBomDeduction },
                new TestCaseEntry { Name = "TC-02: Auto-86 protection disables Menu Items when critical Ingredient is depleted", Test = Test_Auto86Protection },
                new TestCaseEntry { Name = "TC-03: Low-stock triggers Automated External Procurement PO up to Par Level", Test = Test_AutomatedProcurementGeneration },
                new TestCaseEntry { Name = "TC-04: Supplier delivery replenishes stock, updates HACCP lot & flags SDI Price Variance > 5%", Test = Test_SupplierDeliveryAndPriceVariance },
                new TestCaseEntry { Name = "TC-05: Live Financial P&L computes Revenue, Theoretical/Actual Food Cost %, Prime Cost % & Net Income", Test = Test_LiveFinancialPnL },
                new TestCaseEntry { Name = "TC-06: Floor Plan Table check-in binds covers, status and running bill to QR/POS orders", Test = Test_FloorPlanTableAndRunningBill },
                new TestCaseEntry { Name = "TC-07: Waste & Spoilage logging increases Actual Food Cost and computes Theoretical vs. Actual Variance", Test = Test_WasteVariancePnL },
                new TestCaseEntry { Name = "TC-08: HACCP Cold-Chain Monitor flags out-of-range fridge temperatures (> 4.0 C)", Test = Test_HaccpTemperatureCompliance },
                new TestCaseEntry { Name = "TC-09 [MARKET GAP]: AI Multi-Vendor Price Broker selects best supplier quote & applies Weather/Booking factor", Test = Test_AiMultiVendorPriceBroker },
                new TestCaseEntry { Name = "TC-10 [MARKET GAP]: Sentiment-to-Margin Matrix flags High-Margin / Low-Review dishes ('Margin Trap')", Test = Test_SentimentToMarginMatrix },
                new TestCaseEntry { Name = "TC-11 [MARKET GAP]: Zero-Effort UoM Converter translates Supplier Cases into Recipe Grams/Units", Test = Test_ZeroEffortUoMConverter }
            };

            Console.WriteLine("======================================================================");
            Console.WriteLine(" PROTOTYPE FOOD ERP v3.0 - DUAL-APP & MARKET GAP SANDBOX SUITE");
            Console.WriteLine("======================================================================");

            foreach (var t in tests)
            {
                try
                {
                    t.Test();
                    Console.WriteLine(string.Format("[PASS] {0}", t.Name));
                    passed++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[FAIL] {0} -> {1}", t.Name, ex.Message));
                    failed++;
                }
            }

            Console.WriteLine("----------------------------------------------------------------------");
            Console.WriteLine(string.Format(" SUMMARY: {0} Passed, {1} Failed (Total: {2})", passed, failed, tests.Count));
            Console.WriteLine("======================================================================");

            return failed == 0 ? 0 : 1;
        }

        private static AuraRestaurantEngine CreateSeededEngine()
        {
            var engine = new AuraRestaurantEngine
            {
                DailyLaborCost = 400.00m,
                DailyFixedOpEx = 200.00m
            };

            engine.RegisterTable(new RestaurantTable
            {
                TableCode = "T-04",
                ZoneName = "Veranda",
                MaxSeats = 4,
                ActiveCovers = 0,
                Status = TableStatus.Free,
                RunningBill = 0m
            });

            engine.RegisterIngredient(new Ingredient
            {
                Id = "ING-RISO",
                Name = "Riso Carnaroli Riserva",
                Unit = "kg",
                CurrentStock = 2.00m,
                MinThreshold = 5.00m,
                ParLevel = 25.00m,
                UnitCost = 4.50m,
                SupplierId = "SUP-LONGINO",
                AllergenTag = "",
                ActiveLotCode = "LOT-2026-A1"
            });

            engine.RegisterIngredient(new Ingredient
            {
                Id = "ING-ZAFF",
                Name = "Zafferano DOP in Pistilli",
                Unit = "g",
                CurrentStock = 1.00m,
                MinThreshold = 5.00m,
                ParLevel = 20.00m,
                UnitCost = 12.00m,
                SupplierId = "SUP-LONGINO",
                AllergenTag = "",
                ActiveLotCode = "LOT-2026-Z9"
            });

            engine.RegisterIngredient(new Ingredient
            {
                Id = "ING-PARM",
                Name = "Parmigiano Reggiano 36 Mesi",
                Unit = "kg",
                CurrentStock = 4.00m,
                MinThreshold = 3.00m,
                ParLevel = 15.00m,
                UnitCost = 22.00m,
                SupplierId = "SUP-METRO",
                AllergenTag = "Latte / Lattosio (Reg. UE 1169/2011)",
                ActiveLotCode = "LOT-2026-P4"
            });

            engine.RegisterMenuItem(new MenuItem
            {
                Id = "DISH-RISOTTO",
                Name = "Risotto Oro & Zafferano 36m",
                KitchenStation = "Primi",
                SellingPrice = 28.00m,
                PopularityScore = 95,
                ReviewScore = 4.9m,
                ReviewFeedbackNote = "Cremosita perfetta",
                Recipe = new List<RecipeLine>
                {
                    new RecipeLine { IngredientId = "ING-RISO", Quantity = 0.08m },
                    new RecipeLine { IngredientId = "ING-ZAFF", Quantity = 0.50m },
                    new RecipeLine { IngredientId = "ING-PARM", Quantity = 0.04m }
                }
            });

            return engine;
        }

        private static void AssertEqual<T>(T expected, T actual, string context)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(string.Format("{0}: Expected '{1}', got '{2}'", context, expected, actual));
        }

        private static void Test_QrOrderAndBomDeduction()
        {
            var engine = CreateSeededEngine();
            var ticket = engine.SubmitQrOrder("T-04", new List<string> { "DISH-RISOTTO" });

            AssertEqual("KDS-0001", ticket.TicketId, "Ticket ID");
            AssertEqual(OrderStatus.New, ticket.Status, "Initial KDS Status");
            AssertEqual(28.00m, ticket.TotalAmount, "Order Total Revenue");
            AssertEqual(7.24m, ticket.TheoreticalFoodCost, "Theoretical Food Cost");
            AssertEqual(1.92m, engine.Inventory["ING-RISO"].CurrentStock, "Remaining Rice Stock");
            AssertEqual(0.50m, engine.Inventory["ING-ZAFF"].CurrentStock, "Remaining Saffron Stock");
            AssertEqual(true, ticket.AllergenAlerts.Contains("Latte / Lattosio (Reg. UE 1169/2011)"), "EU Allergen Tag");
        }

        private static void Test_Auto86Protection()
        {
            var engine = CreateSeededEngine();
            engine.SubmitQrOrder("T-01", new List<string> { "DISH-RISOTTO" });
            AssertEqual(true, engine.Menu["DISH-RISOTTO"].IsAvailable, "Available after 1st order");

            engine.SubmitQrOrder("T-02", new List<string> { "DISH-RISOTTO" });
            AssertEqual(0.00m, engine.Inventory["ING-ZAFF"].CurrentStock, "Saffron depleted to 0");
            AssertEqual(false, engine.Menu["DISH-RISOTTO"].IsAvailable, "Auto-86 triggered after 2nd order");
        }

        private static void Test_AutomatedProcurementGeneration()
        {
            var engine = CreateSeededEngine();
            var pos = engine.GenerateAutomatedPurchaseOrders();

            AssertEqual(1, pos.Count, "Single consolidated PO for SUP-LONGINO");
            var po = pos[0];
            AssertEqual("SUP-LONGINO", po.SupplierId, "Supplier ID");
            AssertEqual(2, po.Lines.Count, "Two low-stock lines included");

            var risoLine = po.Lines.First(l => l.IngredientId == "ING-RISO");
            AssertEqual(23.00m, risoLine.OrderedQuantity, "Order quantity up to ParLevel (25 - 2)");
        }

        private static void Test_SupplierDeliveryAndPriceVariance()
        {
            var engine = CreateSeededEngine();
            var pos = engine.GenerateAutomatedPurchaseOrders();
            string poNum = pos[0].PoNumber;

            var billedPrices = new Dictionary<string, decimal>
            {
                { "ING-RISO", 4.50m },
                { "ING-ZAFF", 13.80m }
            };

            engine.ReceiveSupplierDelivery(poNum, billedPrices, "LOT-2026-NEW99");

            AssertEqual(25.00m, engine.Inventory["ING-RISO"].CurrentStock, "Stock replenished to ParLevel");
            AssertEqual("LOT-2026-NEW99", engine.Inventory["ING-ZAFF"].ActiveLotCode, "HACCP Lot updated");
            AssertEqual(1, engine.PriceVarianceAlerts.Count, "Price variance alert triggered");
        }

        private static void Test_LiveFinancialPnL()
        {
            var engine = CreateSeededEngine();
            engine.Inventory["ING-ZAFF"].CurrentStock = 100.0m;
            engine.Inventory["ING-RISO"].CurrentStock = 100.0m;
            engine.Inventory["ING-PARM"].CurrentStock = 100.0m;
            engine.RefreshMenuAvailability();

            var batch = Enumerable.Repeat("DISH-RISOTTO", 50).ToList();
            engine.SubmitQrOrder("T-BANQUET", batch);

            var pnl = engine.GetLiveFinancialSnapshot();
            AssertEqual(1400.00m, pnl.GrossRevenue, "Gross Revenue");
            AssertEqual(362.00m, pnl.TheoreticalFoodCost, "Theoretical Food Cost");
            AssertEqual(25.86m, pnl.FoodCostPercentage, "Food Cost %");
            AssertEqual(28.57m, pnl.LaborCostPercentage, "Labor Cost %");
            AssertEqual(54.43m, pnl.PrimeCostPercentage, "Prime Cost %");
            AssertEqual(438.00m, pnl.NetOperatingIncome, "Net Operating Income (EBITDA)");
        }

        private static void Test_FloorPlanTableAndRunningBill()
        {
            var engine = CreateSeededEngine();
            engine.CheckInTable("T-04", 3);
            AssertEqual(TableStatus.Seated, engine.Tables["T-04"].Status, "Table Status Seated");
            AssertEqual(3, engine.Tables["T-04"].ActiveCovers, "Active Covers");

            engine.SubmitQrOrder("T-04", new List<string> { "DISH-RISOTTO" });
            AssertEqual(28.00m, engine.Tables["T-04"].RunningBill, "Table Running Bill updated");
        }

        private static void Test_WasteVariancePnL()
        {
            var engine = CreateSeededEngine();
            engine.Inventory["ING-ZAFF"].CurrentStock = 100.0m;
            engine.Inventory["ING-RISO"].CurrentStock = 100.0m;
            engine.Inventory["ING-PARM"].CurrentStock = 100.0m;
            engine.RefreshMenuAvailability();

            var batch = Enumerable.Repeat("DISH-RISOTTO", 10).ToList();
            engine.SubmitQrOrder("T-04", batch);

            engine.RecordKitchenWaste("ING-PARM", 1.00m);

            var pnl = engine.GetLiveFinancialSnapshot();
            AssertEqual(22.00m, pnl.ActualWasteAndVariance, "Recorded Waste Cost");
            AssertEqual(94.40m, pnl.TotalCogs, "Total Actual COGS");
            AssertEqual(7.85m, pnl.FoodCostVariancePercentage, "Variance % between Actual and Theoretical");
        }

        private static void Test_HaccpTemperatureCompliance()
        {
            var engine = CreateSeededEngine();
            engine.RegisterFridgeReading(new HaccpFridgeReading
            {
                FridgeId = "FR-01",
                Name = "Cella Carne +2C",
                TemperatureCelsius = 2.1m,
                MaxAllowedCelsius = 4.0m
            });
            engine.RegisterFridgeReading(new HaccpFridgeReading
            {
                FridgeId = "FR-02",
                Name = "Cella Pesce Fresco",
                TemperatureCelsius = 6.4m,
                MaxAllowedCelsius = 4.0m
            });

            AssertEqual(true, engine.Fridges[0].IsCompliant, "Fridge 1 Compliant");
            AssertEqual(false, engine.Fridges[1].IsCompliant, "Fridge 2 Out-of-Range Alert");
        }

        private static void Test_AiMultiVendorPriceBroker()
        {
            var engine = CreateSeededEngine();
            engine.RegisterSupplierQuote(new SupplierCatalogQuote { SupplierId = "SUP-LONGINO", SupplierName = "Longino", IngredientId = "ING-RISO", UnitPrice = 4.50m, LeadTimeHours = 24 });
            engine.RegisterSupplierQuote(new SupplierCatalogQuote { SupplierId = "SUP-MARR-DIRECT", SupplierName = "MARR Direct", IngredientId = "ING-RISO", UnitPrice = 3.90m, LeadTimeHours = 18 });

            // Base needed: 25 - 2 = 23kg. Weather/Booking multiplier 1.10 -> 25.30kg at 3.90 EUR/kg
            var po = engine.GenerateSmartBrokerPurchaseOrder("ING-RISO", 1.10m);
            AssertEqual("SUP-MARR-DIRECT", po.SupplierId, "AI Broker picked lowest cost vendor");
            AssertEqual(25.30m, po.Lines[0].OrderedQuantity, "Weather/Booking demand multiplier applied");
            AssertEqual(3.90m, po.Lines[0].UnitPrice, "Lowest catalog price locked");
        }

        private static void Test_SentimentToMarginMatrix()
        {
            var engine = CreateSeededEngine();
            engine.RegisterMenuItem(new MenuItem
            {
                Id = "DISH-TAGLIATA",
                Name = "Tagliata Entrecote",
                SellingPrice = 26.00m,
                ReviewScore = 3.2m,
                ReviewFeedbackNote = "Taglio fibroso nell'ultimo lotto fornitore",
                Recipe = new List<RecipeLine>
                {
                    new RecipeLine { IngredientId = "ING-PARM", Quantity = 0.05m } // Cost 1.10 -> Margin 24.90
                }
            });

            var matrix = engine.AnalyzeSentimentToMarginMatrix();
            var trap = matrix.First(m => m.MenuItemId == "DISH-TAGLIATA");
            AssertEqual(true, trap.IsMarginTrap, "Flagged as Margin Trap (High Margin + Low Review Score < 3.8)");
        }

        private static void Test_ZeroEffortUoMConverter()
        {
            var engine = CreateSeededEngine();
            // Receive 2 Cases of Rice, each with 6 packs of 1.0kg (Total 12kg) billed at 48.00 EUR total (4.00 EUR/kg)
            decimal addedKg = engine.ReceiveCaseInvoiceWithAutoUoMConversion("ING-RISO", 2, 6, 1.0m, 48.00m);
            AssertEqual(12.00m, addedKg, "Converted Cases->Packs->kg");
            AssertEqual(14.00m, engine.Inventory["ING-RISO"].CurrentStock, "Updated Rice Stock (2 + 12)");
            AssertEqual(4.00m, engine.Inventory["ING-RISO"].UnitCost, "Recalculated unit cost per kg");
        }
    }
}
