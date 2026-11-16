using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;
using System.Security.Claims;
using QuanLyQuanAn.Services;
using System.Security.Cryptography;
using System.Text;

namespace QuanLyQuanAn.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;

        public AccountController(AppDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string TaiKhoan,
            string MatKhau,
            bool RememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(TaiKhoan))
                ModelState.AddModelError("TaiKhoan",
                    "Vui lòng nhập tài khoản.");

            if (string.IsNullOrWhiteSpace(MatKhau))
                ModelState.AddModelError("MatKhau",
                    "Vui lòng nhập mật khẩu.");

            if (!ModelState.IsValid)
                return View();

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv =>
                    nv.TaiKhoan == TaiKhoan ||
                    nv.Email == TaiKhoan);

            if (nhanVien == null || nhanVien.MatKhau != MatKhau)
            {
                ModelState.AddModelError("",
                    "Tài khoản hoặc mật khẩu không đúng.");

                return View();
            }

            if (nhanVien.TrangThai != "Hoạt động")
            {
                ModelState.AddModelError("",
                    "Tài khoản hiện không hoạt động.");

                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    nhanVien.MaNV.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    nhanVien.TaiKhoan),

                new Claim("MaNV",
                    nhanVien.MaNV.ToString()),

                new Claim("HoTen",
                    nhanVien.HoTen),

                new Claim("VaiTro",
                    nhanVien.VaiTro)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = RememberMe,
                AllowRefresh = true
            };

            if (RememberMe)
            {
                authProperties.ExpiresUtc =
                    DateTimeOffset.UtcNow.AddDays(14);
            }

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            // Preserve compatibility with existing Session-based code.
            HttpContext.Session.SetInt32(
                "MaNV", nhanVien.MaNV);

            HttpContext.Session.SetString(
                "HoTen", nhanVien.HoTen);

            HttpContext.Session.SetString(
                "VaiTro", nhanVien.VaiTro);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            HttpContext.Session.Clear();

            return RedirectToAction(nameof(Login));
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Vui lòng nhập email.");
                return View();
            }

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.Email == email);

            // Clear any previous recovery attempt.
            ClearPasswordResetSession();

            if (nhanVien != null)
            {
                var code = RandomNumberGenerator
                    .GetInt32(0, 1000000)
                    .ToString("D6");

                var codeHash = Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(code)));

                HttpContext.Session.SetInt32(
                    "PasswordResetMaNV", nhanVien.MaNV);

                HttpContext.Session.SetString(
                    "PasswordResetCodeHash", codeHash);

                HttpContext.Session.SetString(
                    "PasswordResetExpiry",
                    DateTimeOffset.UtcNow.AddMinutes(10)
                        .ToUnixTimeSeconds().ToString());

                HttpContext.Session.SetInt32(
                    "PasswordResetAttempts", 0);

                await _emailSender.SendEmailAsync(
                    nhanVien.Email,
                    "Mã xác nhận khôi phục mật khẩu",
                    $@"
                <h3>Khôi phục mật khẩu</h3>
                <p>Xin chào {System.Net.WebUtility.HtmlEncode(nhanVien.HoTen)},</p>
                <p>Mã xác nhận của bạn là:</p>
                <h2>{code}</h2>
                <p>Mã có hiệu lực trong 10 phút.</p>
                <p>Nếu bạn không yêu cầu khôi phục mật khẩu, hãy bỏ qua email này.</p>"
                );
            }

            return RedirectToAction(nameof(VerifyCode));
        }

        [HttpGet]
        public IActionResult VerifyCode()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyCode(string code)
        {
            var storedHash = HttpContext.Session.GetString(
                "PasswordResetCodeHash");

            var expiryString = HttpContext.Session.GetString(
                "PasswordResetExpiry");

            var attempts = HttpContext.Session.GetInt32(
                "PasswordResetAttempts") ?? 0;

            if (storedHash == null ||
                expiryString == null ||
                !long.TryParse(expiryString, out var expiry) ||
                DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
            {
                ClearPasswordResetSession();

                ModelState.AddModelError(
                    "", "Mã xác nhận đã hết hạn. Vui lòng yêu cầu mã mới.");

                return View();
            }

            if (attempts >= 5)
            {
                ClearPasswordResetSession();

                ModelState.AddModelError(
                    "", "Bạn đã nhập sai quá nhiều lần. Vui lòng yêu cầu mã mới.");

                return View();
            }

            var inputHash = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(code ?? "")));

            var isValid = CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(storedHash),
                Convert.FromHexString(inputHash));

            if (!isValid)
            {
                HttpContext.Session.SetInt32(
                    "PasswordResetAttempts", attempts + 1);

                ModelState.AddModelError(
                    "", $"Mã xác nhận không đúng. Còn {4 - attempts} lần thử.");

                return View();
            }

            HttpContext.Session.SetString(
                "PasswordResetVerified", "true");

            return RedirectToAction(nameof(ResetPassword));
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (HttpContext.Session.GetString(
                    "PasswordResetVerified") != "true")
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            string newPassword,
            string confirmPassword)
        {
            if (HttpContext.Session.GetString(
                    "PasswordResetVerified") != "true")
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (string.IsNullOrWhiteSpace(newPassword) ||
                newPassword.Length < 8)
            {
                ModelState.AddModelError(
                    "newPassword",
                    "Mật khẩu phải có ít nhất 8 ký tự.");

                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(
                    "confirmPassword",
                    "Mật khẩu xác nhận không khớp.");

                return View();
            }

            var maNV = HttpContext.Session.GetInt32(
                "PasswordResetMaNV");

            if (maNV == null)
            {
                ClearPasswordResetSession();
                return RedirectToAction(nameof(ForgotPassword));
            }

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.MaNV == maNV.Value);

            if (nhanVien == null)
            {
                ClearPasswordResetSession();
                return RedirectToAction(nameof(ForgotPassword));
            }

            // Match the existing Login implementation.
            nhanVien.MatKhau = newPassword;

            await _context.SaveChangesAsync();

            ClearPasswordResetSession();

            TempData["Success"] = "Mật khẩu đã được đặt lại thành công.";

            return RedirectToAction(nameof(Login));
        }

        private void ClearPasswordResetSession()
        {
            HttpContext.Session.Remove("PasswordResetMaNV");
            HttpContext.Session.Remove("PasswordResetCodeHash");
            HttpContext.Session.Remove("PasswordResetExpiry");
            HttpContext.Session.Remove("PasswordResetAttempts");
            HttpContext.Session.Remove("PasswordResetVerified");
        }
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
