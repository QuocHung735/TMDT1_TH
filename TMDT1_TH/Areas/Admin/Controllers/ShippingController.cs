using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class ShippingController(
    ApplicationDbContext db) : Controller
{
    private readonly ApplicationDbContext _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        bool? active,
        int? carrierEditId,
        int? serviceEditId,
        CancellationToken cancellationToken)
    {
        var carrierForm = new ShippingCarrierFormViewModel();
        var serviceForm = new ShippingServiceFormViewModel();
        var openCarrierForm = false;
        var openServiceForm = false;

        if (carrierEditId.HasValue)
        {
            var carrier = await _db.ShippingCarriers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == carrierEditId.Value,
                    cancellationToken);

            if (carrier is null)
            {
                TempData["Error"] =
                    "Không tìm thấy đơn vị giao nhận.";

                return RedirectToAction(nameof(Index));
            }

            carrierForm = MapCarrierForm(carrier);
            openCarrierForm = true;
        }

        if (serviceEditId.HasValue)
        {
            var service = await _db.ShippingServices
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == serviceEditId.Value,
                    cancellationToken);

            if (service is null)
            {
                TempData["Error"] =
                    "Không tìm thấy dịch vụ vận chuyển.";

                return RedirectToAction(nameof(Index));
            }

            serviceForm = MapServiceForm(service);
            openServiceForm = true;
        }

        return View(
            await BuildViewModelAsync(
                q,
                active,
                carrierForm,
                serviceForm,
                openCarrierForm,
                openServiceForm,
                cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCarrier(
        ShippingCarrierFormViewModel carrierForm,
        CancellationToken cancellationToken)
    {
        carrierForm.Id = null;
        NormalizeCarrier(carrierForm);

        ModelState.Clear();
        TryValidateModel(carrierForm, "CarrierForm");

        await ValidateCarrierAsync(
            carrierForm,
            cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(
                "Index",
                await BuildViewModelAsync(
                    null,
                    null,
                    carrierForm,
                    new ShippingServiceFormViewModel(),
                    true,
                    false,
                    cancellationToken));
        }

        var carrier = new ShippingCarrier
        {
            Code = carrierForm.Code,
            Name = carrierForm.Name,
            PhoneNumber = Clean(carrierForm.PhoneNumber),
            WebsiteUrl = Clean(carrierForm.WebsiteUrl),
            TrackingUrlTemplate =
                Clean(carrierForm.TrackingUrlTemplate),
            IsActive = carrierForm.IsActive,
            DisplayOrder = carrierForm.DisplayOrder,
            CreatedBy = CurrentUserName()
        };

        _db.ShippingCarriers.Add(carrier);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);

            TempData["Success"] =
                $"Đã tạo đơn vị giao nhận “{carrier.Name}”.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể lưu đơn vị giao nhận. Mã có thể đã tồn tại.");

            return View(
                "Index",
                await BuildViewModelAsync(
                    null,
                    null,
                    carrierForm,
                    new ShippingServiceFormViewModel(),
                    true,
                    false,
                    cancellationToken));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCarrier(
        ShippingCarrierFormViewModel carrierForm,
        CancellationToken cancellationToken)
    {
        if (!carrierForm.Id.HasValue)
        {
            TempData["Error"] =
                "Thiếu mã đơn vị giao nhận.";

            return RedirectToAction(nameof(Index));
        }

        NormalizeCarrier(carrierForm);

        ModelState.Clear();
        TryValidateModel(carrierForm, "CarrierForm");

        await ValidateCarrierAsync(
            carrierForm,
            cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(
                "Index",
                await BuildViewModelAsync(
                    null,
                    null,
                    carrierForm,
                    new ShippingServiceFormViewModel(),
                    true,
                    false,
                    cancellationToken));
        }

        var carrier = await _db.ShippingCarriers
            .FirstOrDefaultAsync(
                x => x.Id == carrierForm.Id.Value,
                cancellationToken);

        if (carrier is null)
            return NotFound();

        carrier.Code = carrierForm.Code;
        carrier.Name = carrierForm.Name;
        carrier.PhoneNumber = Clean(carrierForm.PhoneNumber);
        carrier.WebsiteUrl = Clean(carrierForm.WebsiteUrl);
        carrier.TrackingUrlTemplate =
            Clean(carrierForm.TrackingUrlTemplate);
        carrier.IsActive = carrierForm.IsActive;
        carrier.DisplayOrder = carrierForm.DisplayOrder;
        carrier.UpdatedBy = CurrentUserName();

        try
        {
            await _db.SaveChangesAsync(cancellationToken);

            TempData["Success"] =
                $"Đã cập nhật đơn vị giao nhận “{carrier.Name}”.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể cập nhật đơn vị giao nhận.");

            return View(
                "Index",
                await BuildViewModelAsync(
                    null,
                    null,
                    carrierForm,
                    new ShippingServiceFormViewModel(),
                    true,
                    false,
                    cancellationToken));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCarrier(
        int id,
        CancellationToken cancellationToken)
    {
        var carrier = await _db.ShippingCarriers
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (carrier is null)
            return NotFound();

        carrier.IsActive = !carrier.IsActive;
        carrier.UpdatedBy = CurrentUserName();

        await _db.SaveChangesAsync(cancellationToken);

        TempData["Success"] = carrier.IsActive
            ? $"Đã kích hoạt “{carrier.Name}”."
            : $"Đã tạm tắt “{carrier.Name}”.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateService(
        ShippingServiceFormViewModel serviceForm,
        CancellationToken cancellationToken)
    {
        serviceForm.Id = null;
        NormalizeService(serviceForm);

        ModelState.Clear();
        TryValidateModel(serviceForm, "ServiceForm");

        await ValidateServiceAsync(
            serviceForm,
            cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(
                "Index",
                await BuildViewModelAsync(
                    null,
                    null,
                    new ShippingCarrierFormViewModel(),
                    serviceForm,
                    false,
                    true,
                    cancellationToken));
        }

        var service = new ShippingService
        {
            ShippingCarrierId =
                serviceForm.ShippingCarrierId!.Value,
            Code = serviceForm.Code,
            Name = serviceForm.Name,
            Description = Clean(serviceForm.Description),
            BaseFee = serviceForm.BaseFee,
            EstimatedMinDays =
                serviceForm.EstimatedMinDays,
            EstimatedMaxDays =
                serviceForm.EstimatedMaxDays,
            IsActive = serviceForm.IsActive,
            DisplayOrder = serviceForm.DisplayOrder,
            CreatedBy = CurrentUserName()
        };

        _db.ShippingServices.Add(service);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);

            TempData["Success"] =
                $"Đã tạo dịch vụ “{service.Name}”.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể lưu dịch vụ. Mã dịch vụ có thể đã tồn tại trong đơn vị này.");

            return View(
                "Index",
                await BuildViewModelAsync(
                    null,
                    null,
                    new ShippingCarrierFormViewModel(),
                    serviceForm,
                    false,
                    true,
                    cancellationToken));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditService(
        ShippingServiceFormViewModel serviceForm,
        CancellationToken cancellationToken)
    {
        if (!serviceForm.Id.HasValue)
        {
            TempData["Error"] =
                "Thiếu mã dịch vụ vận chuyển.";

            return RedirectToAction(nameof(Index));
        }

        NormalizeService(serviceForm);

        ModelState.Clear();
        TryValidateModel(serviceForm, "ServiceForm");

        await ValidateServiceAsync(
            serviceForm,
            cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(
                "Index",
                await BuildViewModelAsync(
                    null,
                    null,
                    new ShippingCarrierFormViewModel(),
                    serviceForm,
                    false,
                    true,
                    cancellationToken));
        }

        var service = await _db.ShippingServices
            .FirstOrDefaultAsync(
                x => x.Id == serviceForm.Id.Value,
                cancellationToken);

        if (service is null)
            return NotFound();

        service.ShippingCarrierId =
            serviceForm.ShippingCarrierId!.Value;
        service.Code = serviceForm.Code;
        service.Name = serviceForm.Name;
        service.Description = Clean(serviceForm.Description);
        service.BaseFee = serviceForm.BaseFee;
        service.EstimatedMinDays =
            serviceForm.EstimatedMinDays;
        service.EstimatedMaxDays =
            serviceForm.EstimatedMaxDays;
        service.IsActive = serviceForm.IsActive;
        service.DisplayOrder = serviceForm.DisplayOrder;
        service.UpdatedBy = CurrentUserName();

        try
        {
            await _db.SaveChangesAsync(cancellationToken);

            TempData["Success"] =
                $"Đã cập nhật dịch vụ “{service.Name}”.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể cập nhật dịch vụ vận chuyển.");

            return View(
                "Index",
                await BuildViewModelAsync(
                    null,
                    null,
                    new ShippingCarrierFormViewModel(),
                    serviceForm,
                    false,
                    true,
                    cancellationToken));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleService(
        int id,
        CancellationToken cancellationToken)
    {
        var service = await _db.ShippingServices
            .Include(x => x.ShippingCarrier)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (service is null)
            return NotFound();

        if (!service.IsActive &&
            !service.ShippingCarrier.IsActive)
        {
            TempData["Error"] =
                "Hãy kích hoạt đơn vị giao nhận trước khi bật dịch vụ.";

            return RedirectToAction(nameof(Index));
        }

        service.IsActive = !service.IsActive;
        service.UpdatedBy = CurrentUserName();

        await _db.SaveChangesAsync(cancellationToken);

        TempData["Success"] = service.IsActive
            ? $"Đã kích hoạt dịch vụ “{service.Name}”."
            : $"Đã tạm tắt dịch vụ “{service.Name}”.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<ShippingManagementViewModel>
        BuildViewModelAsync(
            string? queryText,
            bool? active,
            ShippingCarrierFormViewModel carrierForm,
            ShippingServiceFormViewModel serviceForm,
            bool openCarrierForm,
            bool openServiceForm,
            CancellationToken cancellationToken)
    {
        queryText = Clean(queryText);

        var carrierQuery = _db.ShippingCarriers
            .AsNoTracking()
            .AsQueryable();

        var serviceQuery = _db.ShippingServices
            .AsNoTracking()
            .Include(x => x.ShippingCarrier)
            .AsQueryable();

        if (queryText is not null)
        {
            carrierQuery = carrierQuery.Where(x =>
                x.Code.Contains(queryText) ||
                x.Name.Contains(queryText) ||
                (x.PhoneNumber != null &&
                 x.PhoneNumber.Contains(queryText)));

            serviceQuery = serviceQuery.Where(x =>
                x.Code.Contains(queryText) ||
                x.Name.Contains(queryText) ||
                x.ShippingCarrier.Name.Contains(queryText));
        }

        if (active.HasValue)
        {
            carrierQuery = carrierQuery.Where(x =>
                x.IsActive == active.Value);

            serviceQuery = serviceQuery.Where(x =>
                x.IsActive == active.Value);
        }

        var carriers = await carrierQuery
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ShippingCarrierRowViewModel
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                PhoneNumber = x.PhoneNumber,
                WebsiteUrl = x.WebsiteUrl,
                TrackingUrlTemplate =
                    x.TrackingUrlTemplate,
                ServiceCount = x.Services.Count,
                ActiveServiceCount =
                    x.Services.Count(service =>
                        service.IsActive),
                IsActive = x.IsActive,
                DisplayOrder = x.DisplayOrder
            })
            .ToListAsync(cancellationToken);

        var services = await serviceQuery
            .OrderBy(x => x.ShippingCarrier.DisplayOrder)
            .ThenBy(x => x.ShippingCarrier.Name)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ShippingServiceRowViewModel
            {
                Id = x.Id,
                ShippingCarrierId =
                    x.ShippingCarrierId,
                CarrierName =
                    x.ShippingCarrier.Name,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                BaseFee = x.BaseFee,
                EstimatedMinDays =
                    x.EstimatedMinDays,
                EstimatedMaxDays =
                    x.EstimatedMaxDays,
                OrderCount = x.Orders.Count,
                IsActive = x.IsActive,
                CarrierIsActive =
                    x.ShippingCarrier.IsActive,
                DisplayOrder = x.DisplayOrder
            })
            .ToListAsync(cancellationToken);

        var carrierOptions = await _db.ShippingCarriers
            .AsNoTracking()
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.IsActive
                    ? x.Name
                    : $"{x.Name} (đang tắt)"
            })
            .ToListAsync(cancellationToken);

        return new ShippingManagementViewModel
        {
            Query = queryText,
            Active = active,
            CarrierCount =
                await _db.ShippingCarriers
                    .CountAsync(cancellationToken),
            ActiveCarrierCount =
                await _db.ShippingCarriers
                    .CountAsync(
                        x => x.IsActive,
                        cancellationToken),
            ServiceCount =
                await _db.ShippingServices
                    .CountAsync(cancellationToken),
            ActiveServiceCount =
                await _db.ShippingServices
                    .CountAsync(
                        x =>
                            x.IsActive &&
                            x.ShippingCarrier.IsActive,
                        cancellationToken),
            CarrierForm = carrierForm,
            ServiceForm = serviceForm,
            OpenCarrierForm = openCarrierForm,
            OpenServiceForm = openServiceForm,
            Carriers = carriers,
            Services = services,
            CarrierOptions = carrierOptions
        };
    }

    private async Task ValidateCarrierAsync(
        ShippingCarrierFormViewModel form,
        CancellationToken cancellationToken)
    {
        var duplicate = await _db.ShippingCarriers
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.Code == form.Code &&
                    (!form.Id.HasValue ||
                     x.Id != form.Id.Value),
                cancellationToken);

        if (duplicate)
        {
            ModelState.AddModelError(
                "CarrierForm.Code",
                "Mã đơn vị giao nhận đã tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(
                form.TrackingUrlTemplate))
        {
            if (!form.TrackingUrlTemplate.Contains(
                    "{trackingNumber}",
                    StringComparison.Ordinal))
            {
                ModelState.AddModelError(
                    "CarrierForm.TrackingUrlTemplate",
                    "Mẫu URL phải chứa {trackingNumber}.");
            }
            else
            {
                var sample = form.TrackingUrlTemplate.Replace(
                    "{trackingNumber}",
                    "TEST123",
                    StringComparison.Ordinal);

                if (!IsSafeHttpUrl(sample))
                {
                    ModelState.AddModelError(
                        "CarrierForm.TrackingUrlTemplate",
                        "Mẫu URL tra cứu phải là URL http hoặc https hợp lệ.");
                }
            }
        }
    }

    private async Task ValidateServiceAsync(
        ShippingServiceFormViewModel form,
        CancellationToken cancellationToken)
    {
        if (form.EstimatedMaxDays <
            form.EstimatedMinDays)
        {
            ModelState.AddModelError(
                "ServiceForm.EstimatedMaxDays",
                "Số ngày tối đa phải lớn hơn hoặc bằng số ngày tối thiểu.");
        }

        if (!form.ShippingCarrierId.HasValue)
            return;

        var carrier = await _db.ShippingCarriers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.Id ==
                    form.ShippingCarrierId.Value,
                cancellationToken);

        if (carrier is null)
        {
            ModelState.AddModelError(
                "ServiceForm.ShippingCarrierId",
                "Đơn vị giao nhận không tồn tại.");

            return;
        }

        if (form.IsActive && !carrier.IsActive)
        {
            ModelState.AddModelError(
                "ServiceForm.ShippingCarrierId",
                "Không thể bật dịch vụ thuộc đơn vị đang tắt.");
        }

        var duplicate = await _db.ShippingServices
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.ShippingCarrierId ==
                    form.ShippingCarrierId.Value &&
                    x.Code == form.Code &&
                    (!form.Id.HasValue ||
                     x.Id != form.Id.Value),
                cancellationToken);

        if (duplicate)
        {
            ModelState.AddModelError(
                "ServiceForm.Code",
                "Mã dịch vụ đã tồn tại trong đơn vị giao nhận này.");
        }
    }

    private static ShippingCarrierFormViewModel
        MapCarrierForm(ShippingCarrier carrier) =>
        new()
        {
            Id = carrier.Id,
            Code = carrier.Code,
            Name = carrier.Name,
            PhoneNumber = carrier.PhoneNumber,
            WebsiteUrl = carrier.WebsiteUrl,
            TrackingUrlTemplate =
                carrier.TrackingUrlTemplate,
            IsActive = carrier.IsActive,
            DisplayOrder = carrier.DisplayOrder
        };

    private static ShippingServiceFormViewModel
        MapServiceForm(ShippingService service) =>
        new()
        {
            Id = service.Id,
            ShippingCarrierId =
                service.ShippingCarrierId,
            Code = service.Code,
            Name = service.Name,
            Description = service.Description,
            BaseFee = service.BaseFee,
            EstimatedMinDays =
                service.EstimatedMinDays,
            EstimatedMaxDays =
                service.EstimatedMaxDays,
            IsActive = service.IsActive,
            DisplayOrder = service.DisplayOrder
        };

    private static void NormalizeCarrier(
        ShippingCarrierFormViewModel form)
    {
        form.Code = (form.Code ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        form.Name =
            (form.Name ?? string.Empty).Trim();

        form.PhoneNumber =
            Clean(form.PhoneNumber);

        form.WebsiteUrl =
            Clean(form.WebsiteUrl);

        form.TrackingUrlTemplate =
            Clean(form.TrackingUrlTemplate);
    }

    private static void NormalizeService(
        ShippingServiceFormViewModel form)
    {
        form.Code = (form.Code ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        form.Name =
            (form.Name ?? string.Empty).Trim();

        form.Description =
            Clean(form.Description);
    }

    private static bool IsSafeHttpUrl(string value)
    {
        return Uri.TryCreate(
                   value,
                   UriKind.Absolute,
                   out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp ||
                uri.Scheme == Uri.UriSchemeHttps);
    }

    private string CurrentUserName() =>
        User.Identity?.Name ?? "Admin";

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
