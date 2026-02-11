using incode2.Models;
using incode2.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode2.Controllers
{

    public class OrderController : Controller
    {
        private readonly incodedatabaseContext _context;

        public OrderController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: Order
        [HttpGet]
        public ActionResult Index()
        {
            return View(_context.Orders);
        }

        // GET: Order/Create 顯示頁面
        [HttpGet]
        public IActionResult Create()
        {
            // 從資料庫抓出商品（可加條件例如 OnSale、Stock > 0）
            var products = _context.Product.OrderBy(p => p.Name).ToList();
            ViewBag.Products = products;
            return View();
        }

        // POST: Order/Create 新增訂單
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderViewModel NewOrder)
        {
            //if (ModelState.IsValid)
            //{
                var orders = new Orders();
                orders.OrderId = 0;
                orders.UserId = NewOrder.UserId;
                orders.TotalPoint = NewOrder.TotalPoint;
                orders.Status = NewOrder.Status;
                orders.Remark = NewOrder.Remark;
                orders.OrderDate = DateTime.Now;
                _context.Add(orders);
                await _context.SaveChangesAsync();
                foreach (var item in NewOrder.orderdetails)
                {
                    //orderdetai
                    var orderdetail = new Orderdetail();
                    orderdetail.OrderId = orders.OrderId;
                    orderdetail.ProductId = item.ProductId;
                    orderdetail.Quantity = item.Quantity;
                    orderdetail.UnitPointAtPurchase = item.UnitPointAtPurchase;
                    orderdetail.RequiresShipping = item.RequiresShipping;
                    _context.Add(orderdetail);

                    // 扣除庫存
                    var product = await _context.Product.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Stock -= item.Quantity;
                        _context.Update(product);
                    }
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            //}
            //else 
            //{
            //    return View(NewOrder);
            //}
        }

        // GET: Order/Edit/5 顯示頁面
        [Route("/Order/Edit/{OrderId}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int OrderId)
        {
            var Order = await _context.Orders.FindAsync(OrderId);
            if (Order == null)
            {
                return NotFound();
            }

            var orderdetail = new OrderViewModel();
            orderdetail.OrderId = Order.OrderId; // 記得加上這一行，表單提交才抓得到 ID
            orderdetail.UserId = Order.UserId;
            orderdetail.TotalPoint = Order.TotalPoint;
            orderdetail.Status = Order.Status;
            orderdetail.Remark = Order.Remark;

            orderdetail.orderdetails = await _context.Orderdetail
                .Where(od => od.OrderId == OrderId)
                .ToListAsync();

            var products = _context.Product.OrderBy(p => p.Name).ToList();
            ViewBag.Products = products;
            return View(orderdetail);
        }

        // POST: Order/Edit/5 修改訂單
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OrderViewModel EditOrder, int OrderId)
        {
            if (OrderId != EditOrder.OrderId)
            {
                return NotFound();
            }

            // 先從 DB 讀出原本的訂單，保留 OrderDate（避免 DateTime.MinValue 寫入）
            var orders = await _context.Orders.FindAsync(OrderId);
            if (orders == null)
            {
                return NotFound();
            }

            if (orders.Status == "已取消" && EditOrder.Status != "已取消")
            {
                // 從已取消改回其他狀態，清除取消時間
                orders.CanceledAt = null;
            }

            orders.UserId = EditOrder.UserId;
            orders.TotalPoint = EditOrder.TotalPoint;
            orders.Status = EditOrder.Status;
            orders.Remark = EditOrder.Remark;

            // 若狀態為取消，設定取消時間；否則清除（或保留原值視需求）
            if (orders.Status == "已取消")
            {
                // 只有當原本不是取消狀態（CanceledAt 為 null）時才執行退貨邏輯
                // 這樣可以避免重複操作導致庫存重複增加
                if (orders.CanceledAt == null)
                {
                    orders.CanceledAt = DateTime.Now;

                    // 取出目前資料庫中的明細，不管前端傳來的修改，直接將原單庫存全數退還
                    var currentDetails = await _context.Orderdetail
                        .Where(od => od.OrderId == OrderId)
                        .ToListAsync();

                    foreach (var detail in currentDetails)
                    {
                        var product = await _context.Product.FindAsync(detail.ProductId);
                        if (product != null)
                        {
                            product.Stock += detail.Quantity;
                        }
                    }
                }
                _context.Orders.Update(orders);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                orders.CanceledAt = null;
            }

            _context.Orders.Update(orders);
            // 這裡可以先不SaveChanges，等明細處理完一次Save

            // ===== 同步 OrderDetail（比較並執行 CRUD） =====
            // 取得 DB 的原始明細（tracked entities）
            var existingDetails = await _context.Orderdetail
                .Where(od => od.OrderId == OrderId)
                .ToListAsync();

            var submitted = EditOrder.orderdetails ?? new List<Orderdetail>();

            // 記錄已處理的 existing id（用於找出要刪除的）
            var processedIds = new HashSet<int>();

            foreach (var item in submitted)
            {
                // 若有帶 ID（編輯現有明細）
                if (item.OrderdetailId > 0)
                {
                    var exist = existingDetails.FirstOrDefault(e => e.OrderdetailId == item.OrderdetailId);
                    if (exist != null)
                    {
                        // --- 庫存處理邏輯 (Edit) ---
                        if (exist.ProductId != item.ProductId)
                        {
                            // 情境A：商品換了 (例如從 A商品 換成 B商品)
                            // 1. 把舊商品的庫存補回來
                            var oldProduct = await _context.Product.FindAsync(exist.ProductId);
                            if (oldProduct != null)
                            {
                                oldProduct.Stock += exist.Quantity; 
                            }
                            
                            // 2. 扣除新商品的庫存
                            var newProduct = await _context.Product.FindAsync(item.ProductId);
                            if (newProduct != null)
                            {
                                newProduct.Stock -= item.Quantity;
                            }
                        }
                        else
                        {
                            // 情境B：商品沒變，只改數量
                            var diff = item.Quantity - exist.Quantity; // 差異量 (例如原本 2 改成 5，diff = 3)
                            
                            if (diff != 0)
                            {
                                var product = await _context.Product.FindAsync(exist.ProductId);
                                if (product != null)
                                {
                                    product.Stock -= diff; // 扣除差異 (若 diff 為負，即代表退回庫存)
                                }
                            }
                        }
                        // -------------------------

                        // 更新欄位（entity 已被 context 追蹤）
                        exist.ProductId = item.ProductId;
                        exist.Quantity = item.Quantity;
                        exist.UnitPointAtPurchase = item.UnitPointAtPurchase;
                        exist.RequiresShipping = item.RequiresShipping;

                        processedIds.Add(exist.OrderdetailId);
                    }
                    else
                    {
                        // 傳來 ID 但 DB 找不到，視為新增（容錯）
                        // --- 庫存處理 (Add) ---
                        var product = await _context.Product.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            product.Stock -= item.Quantity;
                        }
                        // ---------------------

                        var newDetail = new Orderdetail
                        {
                            OrderId = OrderId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPointAtPurchase = item.UnitPointAtPurchase,
                            RequiresShipping = item.RequiresShipping
                        };
                        _context.Orderdetail.Add(newDetail);
                        // 無法直接加入 processedIds（新增後才有 Id）
                    }
                }
                else
                {
                    // 新增明細
                    // --- 庫存處理 (Add) ---
                    var product = await _context.Product.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Stock -= item.Quantity;
                    }
                    // ---------------------

                    var newDetail = new Orderdetail
                    {
                        OrderId = OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPointAtPurchase = item.UnitPointAtPurchase,
                        RequiresShipping = item.RequiresShipping
                    };
                    _context.Orderdetail.Add(newDetail);
                }
            }

            // 刪除 DB 中但未被 submitted 處理到的明細
            var toDelete = existingDetails
                .Where(e => !processedIds.Contains(e.OrderdetailId))
                .ToList();

            if (toDelete.Any())
            {
                // --- 庫存處理 (Delete) ---
                foreach (var del in toDelete)
                {
                    // 刪除訂單明細，需把庫存加回去
                    var product = await _context.Product.FindAsync(del.ProductId);
                    if (product != null)
                    {
                        product.Stock += del.Quantity;
                    }
                }
                // -------------------------

                _context.Orderdetail.RemoveRange(toDelete);
            }

            await _context.SaveChangesAsync();
            // ===============================================

            return RedirectToAction(nameof(Index));
        }
    }
}
