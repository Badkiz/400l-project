// ═══════════════════════════════════════════════════════════════
//  HostelMS config.js  —  v2.0 Final
//  Fixes: CORS, sidebar collapse, animations, mock Paystack
// ═══════════════════════════════════════════════════════════════

const API_BASE = "http://localhost:5000/api";
const HUB_URL  = "http://localhost:5000/hubs/rooms";

const STUDENT_BGS = [
  "https://thumbs.dreamstime.com/b/backpackers-hostel-modern-bunk-beds-dorm-room-twelve-people-inside-79935795.jpg",
  "https://thumbs.dreamstime.com/b/big-window-dorm-room-student-european-hostel-level-beds-prague-table-one-chair-prague-receives-more-than-41238336.jpg",
  "https://img.freepik.com/free-photo/young-friends-hostel_52683-121730.jpg?semt=ais_hybrid&w=740&q=80"
];
const ADMIN_BG = "https://images.unsplash.com/photo-1492724441997-5dc865305da7?auto=format&fit=crop&w=1920&q=80";

// ── Auth ──────────────────────────────────────────────────────
function getToken()  { return localStorage.getItem("hms_token"); }
function getUser()   { const u = localStorage.getItem("hms_user"); return u ? JSON.parse(u) : null; }
function saveAuth(d) {
  localStorage.setItem("hms_token", d.token);
  localStorage.setItem("hms_user", JSON.stringify({
    id: d.userId, name: d.fullName, email: d.email, role: d.role
  }));
}
function clearAuth()  { localStorage.removeItem("hms_token"); localStorage.removeItem("hms_user"); }
function isLoggedIn() { return !!getToken(); }
function requireAuth(role) {
  const u = getUser();
  if (!isLoggedIn() || !u) { window.location.href = "login.html"; return false; }
  if (role && u.role !== role) {
    window.location.href = u.role === "Admin" ? "admin-dashboard.html" : "student-dashboard.html";
    return false;
  }
  return true;
}
function doLogout() {
  clearAuth();
  window.location.replace("login.html");
}

// ── API fetch ─────────────────────────────────────────────────
async function apiFetch(path, opts = {}) {
  const headers = { "Content-Type": "application/json", ...(opts.headers || {}) };
  const t = getToken();
  if (t) headers["Authorization"] = "Bearer " + t;
  try {
    const res = await fetch(API_BASE + path, { ...opts, headers });
    if (res.status === 401) { clearAuth(); window.location.href = "login.html"; return null; }
    const ct   = res.headers.get("content-type") || "";
    const data = ct.includes("application/json") ? await res.json() : null;
    if (!res.ok) throw new Error((data && data.message) || "Error " + res.status);
    return data;
  } catch (err) {
    if (err.message && err.message.includes("Failed to fetch")) {
      throw new Error("Cannot reach server. Make sure the backend is running on port 5000.");
    }
    throw err;
  }
}

// ── Toast ─────────────────────────────────────────────────────
function showToast(msg, type) {
  type = type || "info";
  const colours = { success:"#1a6b3a", error:"#5C0A0A", info:"#1a3a6b", warning:"#7c4a00" };
  const el = document.createElement("div");
  el.style.cssText = [
    "position:fixed","top:20px","right:20px","z-index:9999",
    "background:" + (colours[type] || colours.info),
    "color:#fff","padding:13px 20px","border-radius:9px","font-size:13.5px",
    "font-family:'DM Sans',sans-serif","box-shadow:0 8px 32px rgba(0,0,0,.35)",
    "opacity:1","transition:opacity .4s,transform .4s","max-width:340px",
    "line-height:1.5","border-left:4px solid rgba(255,255,255,.3)",
    "transform:translateX(0)"
  ].join(";");
  el.textContent = msg;
  document.body.appendChild(el);
  setTimeout(function(){ el.style.opacity="0"; el.style.transform="translateX(20px)"; setTimeout(function(){ el.remove(); }, 400); }, 3500);
}

// ── TTS ───────────────────────────────────────────────────────
var ttsEnabled = false;
function speak(text) {
  if (!ttsEnabled || !window.speechSynthesis) return;
  window.speechSynthesis.cancel();
  var u = new SpeechSynthesisUtterance(text);
  u.rate = 0.95;
  window.speechSynthesis.speak(u);
}
function toggleTTS() {
  ttsEnabled = !ttsEnabled;
  var b = document.getElementById("tts-btn");
  if (b) {
    b.querySelector("span").textContent = ttsEnabled ? "🔊" : "🔇";
    b.style.background = ttsEnabled ? "rgba(92,10,10,.7)" : "";
  }
  speak(ttsEnabled ? "Text to speech enabled." : "Text to speech disabled.");
}

// ── Formatters ────────────────────────────────────────────────
function formatMoney(v) {
  return new Intl.NumberFormat("en-NG", { style:"currency", currency:"NGN", minimumFractionDigits:0 }).format(v);
}
function formatDate(iso) {
  if (!iso) return "—";
  return new Date(iso).toLocaleDateString("en-NG", { day:"numeric", month:"short", year:"numeric", hour:"2-digit", minute:"2-digit" });
}
function formatDateShort(iso) {
  if (!iso) return "—";
  return new Date(iso).toLocaleDateString("en-NG", { day:"numeric", month:"short", year:"numeric" });
}
function badgeClass(s) {
  return s === "Success" || s === "Active" ? "badge-success" : s === "Pending" ? "badge-warning" : "badge-danger";
}

// ── Nav ───────────────────────────────────────────────────────
var STUDENT_NAV = [
  { label:"Dashboard",          icon:"⊡", href:"student-dashboard.html" },
  { label:"Browse Rooms",       icon:"🚪", href:"rooms.html" },
  { label:"Hostel Info",        icon:"📋", href:"student-info.html" },
  { label:"Schedule / Events",  icon:"📅", href:"student-schedule.html" },
  { label:"Messages",           icon:"💬", href:"student-messages.html" },
  { label:"Settings",           icon:"⚙",  href:"student-settings.html" }
];
var ADMIN_NAV = [
  { label:"Dashboard",     icon:"⊡", href:"admin-dashboard.html" },
  { label:"Manage Rooms",  icon:"🚪", href:"admin-rooms.html" },
  { label:"Allocations",   icon:"📌", href:"admin-allocations.html" },
  { label:"Announcements", icon:"📢", href:"admin-announcements.html" },
  { label:"Schedule",      icon:"📅", href:"admin-schedule.html" },
  { label:"Students",      icon:"👥", href:"admin-students.html" },
  { label:"Messages",      icon:"💬", href:"admin-messages.html" }
];

// ── Layout renderer ───────────────────────────────────────────
function renderLayout(pageTitle, navItems, contentHtml, activeHref) {
  var user    = getUser();
  var isAdmin = user && user.role === "Admin";
  var name    = (user && user.name) ? user.name : "User";
  var ini     = name.split(" ").map(function(w){ return w[0]; }).slice(0,2).join("").toUpperCase();
  var bgUrl   = isAdmin ? ADMIN_BG : STUDENT_BGS[0];

  var navHtml = navItems.map(function(n) {
    var active = n.href === activeHref ? " active" : "";
    return '<a href="' + n.href + '" class="nav-item' + active + '">' +
           '<span class="nav-icon">' + n.icon + '</span>' +
           '<span class="nav-label">' + n.label + '</span>' +
           '</a>';
  }).join("");

  document.documentElement.innerHTML =
'<!DOCTYPE html>\n' +
'<html lang="en">\n' +
'<head>\n' +
'<meta charset="UTF-8"/>\n' +
'<meta name="viewport" content="width=device-width,initial-scale=1.0"/>\n' +
'<title>' + pageTitle + ' \u2013 Hostel MS</title>\n' +
'<link href="https://fonts.googleapis.com/css2?family=DM+Sans:wght@300;400;500;600;700&family=Playfair+Display:wght@600;700&display=swap" rel="stylesheet"/>\n' +
'<style>\n' +
CSS_VARIABLES +
BASE_CSS +
ANIMATION_CSS +
'</style>\n' +
'</head>\n' +
'<body>\n' +
'<div class="bg-layer" id="bg-a" style="background-image:url(\'' + bgUrl + '\')"></div>\n' +
'<div class="bg-layer hidden" id="bg-b"></div>\n' +
'<div class="bg-overlay"></div>\n' +
'<div class="sb-overlay" id="sb-overlay"></div>\n' +
'<aside class="sidebar" id="sidebar">\n' +
'  <div class="sb-logo">\n' +
'    <div class="sb-icon">🏠</div>\n' +
'    <div class="sb-text"><div class="sb-brand">HostelMS</div><div class="sb-sub">Management System</div></div>\n' +
'    <button class="cbtn" id="cbtn" title="Collapse sidebar" aria-label="Toggle sidebar">&#9664;</button>\n' +
'  </div>\n' +
'  <nav class="nav-area">\n' +
'    <div class="nav-lbl">' + (isAdmin ? "Admin" : "Student") + ' Menu</div>\n' +
     navHtml +
'  </nav>\n' +
'  <div class="sb-footer">\n' +
'    <div class="avatar">' + ini + '</div>\n' +
'    <div class="footer-info">\n' +
'      <div class="footer-name">' + name.split(" ")[0] + '</div>\n' +
'      <div class="footer-role">' + (user ? user.role : "") + '</div>\n' +
'    </div>\n' +
'    <button class="logout-btn" onclick="doLogout()" title="Sign out">&#10005;</button>\n' +
'  </div>\n' +
'</aside>\n' +
'<div class="main-area" id="main-area">\n' +
'  <header class="topbar">\n' +
'    <div class="topbar-left">\n' +
'      <button class="menu-toggle" id="menu-toggle" aria-label="Open menu">&#9776;</button>\n' +
'      <div class="page-heading">' + pageTitle + '</div>\n' +
'      <div class="tdiv"></div>\n' +
'      <span class="tbc">' + (isAdmin ? "Admin" : "Student") + ' Portal</span>\n' +
'    </div>\n' +
'    <div class="topbar-right">\n' +
'      <button class="tts-btn" id="tts-btn" onclick="toggleTTS()" title="Text-to-Speech"><span>🔇</span></button>\n' +
'      <div class="topbar-user"><div class="u-dot">' + ini + '</div><span>' + name.split(" ")[0] + '</span></div>\n' +
'    </div>\n' +
'  </header>\n' +
'  <main class="page-content" id="page-content">' + contentHtml + '</main>\n' +
'</div>\n' +
'<script>\n' +
'(function(){\n' +
'  var sb = document.getElementById("sidebar");\n' +
'  var ma = document.getElementById("main-area");\n' +
'  var ov = document.getElementById("sb-overlay");\n' +
'  var cb = document.getElementById("cbtn");\n' +
'  // Restore collapsed state\n' +
'  if (localStorage.getItem("hms_sb") === "1") {\n' +
'    sb.classList.add("collapsed");\n' +
'    ma.classList.add("sb-col");\n' +
'  }\n' +
'  // Desktop collapse toggle\n' +
'  cb.addEventListener("click", function() {\n' +
'    var c = sb.classList.toggle("collapsed");\n' +
'    ma.classList.toggle("sb-col", c);\n' +
'    localStorage.setItem("hms_sb", c ? "1" : "0");\n' +
'  });\n' +
'  // Mobile sidebar\n' +
'  document.getElementById("menu-toggle").addEventListener("click", function() {\n' +
'    sb.classList.toggle("mob-open");\n' +
'    ov.classList.toggle("show");\n' +
'  });\n' +
'  ov.addEventListener("click", function() {\n' +
'    sb.classList.remove("mob-open");\n' +
'    ov.classList.remove("show");\n' +
'  });\n' +
'  // Background slideshow for student pages\n' +
'  var bgs = ' + JSON.stringify(STUDENT_BGS) + ';\n' +
'  var isStudent = ' + (!isAdmin) + ';\n' +
'  if (isStudent && bgs.length > 1) {\n' +
'    var layA = document.getElementById("bg-a");\n' +
'    var layB = document.getElementById("bg-b");\n' +
'    var idx  = 0, active = layA, inactive = layB;\n' +
'    setInterval(function() {\n' +
'      idx = (idx + 1) % bgs.length;\n' +
'      inactive.style.backgroundImage = "url(" + bgs[idx] + ")";\n' +
'      inactive.classList.remove("hidden");\n' +
'      active.classList.add("hidden");\n' +
'      var tmp = active; active = inactive; inactive = tmp;\n' +
'    }, 8000);\n' +
'  }\n' +
'  // Animate page content entrance\n' +
'  var pc = document.getElementById("page-content");\n' +
'  if (pc) { pc.style.opacity="0"; pc.style.transform="translateY(12px)"; setTimeout(function(){ pc.style.transition="opacity .4s ease,transform .4s ease"; pc.style.opacity="1"; pc.style.transform="translateY(0)"; }, 30); }\n' +
'})();\n' +
'<\/script>\n' +
'</body></html>';
}

// ── CSS blocks ────────────────────────────────────────────────
var CSS_VARIABLES = [
':root{',
'--p:#5C0A0A;--pd:#420707;--pm:#7a1414;--pmut:#c97a7a;--pl:#f9e8e8;',
'--bg:#fff;--bg2:#f5f5f5;--bg3:#ececec;',
'--text:#1a1a1a;--text2:#4a4a4a;--muted:#8a8a8a;',
'--bdr:rgba(0,0,0,.1);--bdrlt:rgba(0,0,0,.05);',
'--green:#1a6b3a;--gbg:#e6f4ec;--amber:#7c4a00;--abg:#fef3dd;',
'--red:#5C0A0A;--rbg:#fde8e8;--blue:#1a3a6b;--bbg:#e6ecf4;',
'--card:rgba(255,255,255,.88);',
'}',
].join("");

var BASE_CSS = [
'*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}',
'html,body{height:100%}',
'body{font-family:"DM Sans",sans-serif;background:#1a0808;color:var(--text);min-height:100vh;position:relative}',
// Background layers
'.bg-layer{position:fixed;inset:0;z-index:0;background-size:cover;background-position:center;background-attachment:fixed;transition:opacity 2.5s ease-in-out}',
'.bg-layer.hidden{opacity:0}',
'.bg-overlay{position:fixed;inset:0;z-index:1;background:rgba(10,3,3,.52);pointer-events:none}',
// Sidebar
'.sidebar{position:fixed;top:0;left:0;height:100vh;width:240px;background:rgba(18,4,4,.85);backdrop-filter:blur(24px);-webkit-backdrop-filter:blur(24px);border-right:1px solid rgba(255,255,255,.08);display:flex;flex-direction:column;z-index:100;transition:width .28s cubic-bezier(.4,0,.2,1);overflow:hidden;will-change:width}',
'.sidebar.collapsed{width:58px}',
'.sidebar.collapsed .sb-text,.sidebar.collapsed .nav-lbl,.sidebar.collapsed .nav-label,.sidebar.collapsed .footer-info,.sidebar.collapsed .logout-btn{display:none!important}',
'.sidebar.collapsed .nav-item{justify-content:center;padding:10px 0}',
'.sidebar.collapsed .sb-logo{justify-content:center;padding:18px 0}',
'.sidebar.collapsed .sb-footer{justify-content:center;padding:12px 4px;gap:0}',
'.sidebar.collapsed .cbtn{transform:translateY(-50%) scaleX(-1)}',
'.sb-logo{padding:18px 14px 14px;border-bottom:1px solid rgba(255,255,255,.07);display:flex;align-items:center;gap:9px;position:relative;flex-shrink:0}',
'.sb-icon{width:30px;height:30px;background:rgba(255,255,255,.15);border-radius:7px;display:flex;align-items:center;justify-content:center;font-size:14px;flex-shrink:0}',
'.sb-brand{font-family:"Playfair Display",serif;font-size:14px;font-weight:700;color:#fff;white-space:nowrap}',
'.sb-sub{font-size:9px;color:rgba(255,255,255,.38);white-space:nowrap;margin-top:1px}',
'.cbtn{background:rgba(92,10,10,.95);border:1.5px solid rgba(255,255,255,.2);color:#fff;width:18px;height:18px;border-radius:50%;display:flex;align-items:center;justify-content:center;cursor:pointer;font-size:8px;position:absolute;right:-9px;top:50%;transform:translateY(-50%);z-index:10;box-shadow:0 2px 8px rgba(0,0,0,.5);transition:transform .28s cubic-bezier(.4,0,.2,1),background .15s;flex-shrink:0}',
'.cbtn:hover{background:#5C0A0A}',
'.nav-area{flex:1;overflow-y:auto;overflow-x:hidden;padding:8px 5px;scrollbar-width:none}',
'.nav-area::-webkit-scrollbar{display:none}',
'.nav-lbl{font-size:9px;font-weight:700;letter-spacing:1.2px;text-transform:uppercase;color:rgba(255,255,255,.3);padding:0 9px;margin:4px 0 5px;white-space:nowrap}',
'.nav-item{display:flex;align-items:center;gap:8px;padding:8px 9px;border-radius:7px;margin-bottom:2px;text-decoration:none;color:rgba(255,255,255,.6);font-size:12.5px;font-weight:500;transition:background .14s,color .14s,transform .1s;position:relative;overflow:hidden}',
'.nav-item:hover{background:rgba(255,255,255,.1);color:#fff;transform:translateX(2px)}',
'.nav-item.active{background:rgba(255,255,255,.17);color:#fff;font-weight:600}',
'.nav-item.active::before{content:"";position:absolute;left:0;top:20%;bottom:20%;width:3px;background:#fff;border-radius:0 3px 3px 0}',
'.nav-icon{font-size:14px;width:17px;text-align:center;flex-shrink:0}',
'.nav-label{white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
'.sb-footer{padding:10px 12px;border-top:1px solid rgba(255,255,255,.07);display:flex;align-items:center;gap:8px;flex-shrink:0}',
'.avatar{width:26px;height:26px;border-radius:50%;background:rgba(255,255,255,.2);display:flex;align-items:center;justify-content:center;font-size:10px;font-weight:700;color:#fff;flex-shrink:0;border:1.5px solid rgba(255,255,255,.25)}',
'.footer-name{font-size:11px;font-weight:600;color:#fff;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;max-width:108px}',
'.footer-role{font-size:9.5px;color:rgba(255,255,255,.38)}',
'.logout-btn{margin-left:auto;width:22px;height:22px;border-radius:5px;background:rgba(255,60,60,.18);border:1px solid rgba(255,100,100,.15);color:rgba(255,160,160,.9);cursor:pointer;font-size:11px;display:flex;align-items:center;justify-content:center;transition:background .13s;flex-shrink:0}',
'.logout-btn:hover{background:rgba(255,60,60,.4);color:#fff}',
// Main area
'.main-area{margin-left:240px;min-height:100vh;display:flex;flex-direction:column;position:relative;z-index:2;transition:margin-left .28s cubic-bezier(.4,0,.2,1)}',
'.main-area.sb-col{margin-left:58px}',
// Topbar
'.topbar{background:rgba(255,255,255,.8);backdrop-filter:blur(20px);-webkit-backdrop-filter:blur(20px);border-bottom:1px solid rgba(255,255,255,.4);padding:0 20px;height:52px;display:flex;align-items:center;justify-content:space-between;position:sticky;top:0;z-index:50;box-shadow:0 2px 16px rgba(0,0,0,.1)}',
'.topbar-left{display:flex;align-items:center;gap:9px}',
'.page-heading{font-family:"Playfair Display",serif;font-size:15.5px;font-weight:700;color:#1a1a1a}',
'.tdiv{width:1px;height:14px;background:rgba(0,0,0,.1)}',
'.tbc{font-size:11px;color:#888}',
'.topbar-right{display:flex;align-items:center;gap:6px}',
'.tts-btn{background:rgba(92,10,10,.07);border:1px solid rgba(92,10,10,.12);width:30px;height:30px;border-radius:7px;cursor:pointer;font-size:13px;display:flex;align-items:center;justify-content:center;color:var(--p);transition:all .13s}',
'.tts-btn:hover{background:#f9e8e8}',
'.topbar-user{display:flex;align-items:center;gap:5px;padding:3px 10px;background:rgba(92,10,10,.07);border:1px solid rgba(92,10,10,.1);border-radius:99px;font-size:11.5px;color:#3a3a3a}',
'.u-dot{width:18px;height:18px;background:#5C0A0A;border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:8px;font-weight:700;color:#fff}',
'.menu-toggle{display:none;background:none;border:none;cursor:pointer;font-size:17px;color:#444;padding:3px;border-radius:5px;transition:background .13s}',
'.menu-toggle:hover{background:rgba(0,0,0,.06)}',
// Page content
'.page-content{padding:18px 20px;flex:1}',
// Cards
'.card{background:var(--card);backdrop-filter:blur(18px);-webkit-backdrop-filter:blur(18px);border-radius:11px;padding:18px;border:1px solid rgba(255,255,255,.5);box-shadow:0 4px 20px rgba(0,0,0,.1);transition:box-shadow .15s,transform .15s}',
'.card:hover{box-shadow:0 6px 28px rgba(0,0,0,.14)}',
'.card-sm{padding:12px 14px}',
// Stat cards
'.stat-card{background:var(--card);backdrop-filter:blur(18px);-webkit-backdrop-filter:blur(18px);border-radius:11px;padding:16px 18px;border:1px solid rgba(255,255,255,.5);box-shadow:0 4px 20px rgba(0,0,0,.1);border-top:3px solid var(--accent,#5C0A0A);transition:box-shadow .15s,transform .15s}',
'.stat-card:hover{box-shadow:0 8px 30px rgba(0,0,0,.18);transform:translateY(-2px)}',
'.stat-label{font-size:10px;font-weight:700;letter-spacing:.9px;text-transform:uppercase;color:#777}',
'.stat-value{font-family:"Playfair Display",serif;font-size:22px;font-weight:700;color:#1a1a1a;margin-top:4px;line-height:1}',
'.stat-sub{font-size:11px;color:#888;margin-top:3px}',
'.section-title{font-family:"Playfair Display",serif;font-size:14px;font-weight:700;color:#1a1a1a;margin-bottom:11px}',
// Badges
'.badge{display:inline-flex;align-items:center;padding:2px 8px;border-radius:20px;font-size:11px;font-weight:600}',
'.badge-success{background:rgba(230,244,236,.92);color:#1a6b3a}',
'.badge-warning{background:rgba(254,243,221,.92);color:#7c4a00}',
'.badge-danger{background:rgba(253,232,232,.92);color:#5C0A0A}',
'.badge-info{background:rgba(230,236,244,.92);color:#1a3a6b}',
'.badge-primary{background:#f9e8e8;color:#5C0A0A}',
// Buttons
'.btn{display:inline-flex;align-items:center;gap:5px;padding:7px 15px;border-radius:7px;font-size:12.5px;font-weight:600;cursor:pointer;border:none;transition:all .15s;font-family:"DM Sans",sans-serif;text-decoration:none;white-space:nowrap}',
'.btn-primary{background:#5C0A0A;color:#fff;box-shadow:0 2px 8px rgba(92,10,10,.25)}',
'.btn-primary:hover{background:#420707;box-shadow:0 4px 14px rgba(92,10,10,.35);transform:translateY(-1px)}',
'.btn-primary:active{transform:translateY(0);box-shadow:0 1px 4px rgba(92,10,10,.2)}',
'.btn-secondary{background:rgba(255,255,255,.8);color:#444;border:1px solid rgba(0,0,0,.1)}',
'.btn-secondary:hover{background:rgba(255,255,255,.98)}',
'.btn-danger{background:rgba(253,232,232,.9);color:#5C0A0A;border:1px solid rgba(92,10,10,.1)}',
'.btn-danger:hover{background:#fbd0d0}',
'.btn-sm{padding:5px 11px;font-size:12px;border-radius:6px}',
'.btn:disabled{opacity:.5;cursor:not-allowed;transform:none!important}',
// Form
'.form-input{width:100%;border:1.5px solid rgba(0,0,0,.1);border-radius:7px;padding:9px 11px;font-size:13px;font-family:"DM Sans",sans-serif;color:#1a1a1a;background:rgba(255,255,255,.88);outline:none;transition:border-color .14s,box-shadow .14s,background .14s}',
'.form-input:focus{border-color:#5C0A0A;box-shadow:0 0 0 3px rgba(92,10,10,.09);background:rgba(255,255,255,.96)}',
'.form-input::placeholder{color:#bbb}',
'.form-input:disabled{background:rgba(255,255,255,.4);cursor:not-allowed;color:#999}',
'.form-label{display:block;font-size:11.5px;font-weight:600;color:#555;margin-bottom:4px}',
'.form-group{margin-bottom:12px}',
// Table
'.data-table{width:100%;border-collapse:collapse;font-size:12.5px}',
'.data-table thead th{text-align:left;padding:9px 12px;font-size:9.5px;font-weight:700;letter-spacing:.8px;text-transform:uppercase;color:#888;background:rgba(245,245,245,.88);border-bottom:2px solid rgba(0,0,0,.06)}',
'.data-table tbody tr{border-bottom:1px solid rgba(0,0,0,.04);transition:background .1s}',
'.data-table tbody tr:last-child{border-bottom:none}',
'.data-table tbody tr:hover{background:rgba(255,255,255,.65)}',
'.data-table td{padding:9px 12px;color:#3a3a3a;vertical-align:middle}',
// Modal
'.modal-overlay{display:none;position:fixed;inset:0;background:rgba(0,0,0,.65);backdrop-filter:blur(6px);z-index:200;align-items:center;justify-content:center;padding:20px;animation:fadeIn .2s ease}',
'.modal-overlay.open{display:flex}',
'.modal-box{background:rgba(255,255,255,.96);backdrop-filter:blur(20px);border-radius:12px;padding:24px;width:100%;max-width:500px;box-shadow:0 24px 64px rgba(0,0,0,.3);border:1px solid rgba(255,255,255,.6);animation:slideUp .25s ease}',
'.modal-title{font-family:"Playfair Display",serif;font-size:16px;font-weight:700;color:#1a1a1a;margin-bottom:14px}',
'.progress-track{height:5px;background:rgba(0,0,0,.08);border-radius:99px;overflow:hidden}',
'.progress-fill{height:100%;border-radius:99px;transition:width .4s ease}',
'.empty-state{text-align:center;padding:44px 24px;color:#888}',
'.empty-state .icon{font-size:32px;margin-bottom:8px;opacity:.5}',
'.empty-state p{font-size:13px;line-height:1.6}',
// Overlay
'.sb-overlay{display:none;position:fixed;inset:0;background:rgba(0,0,0,.55);z-index:99;transition:opacity .2s}',
'.sb-overlay.show{display:block}',
// Mobile
'@media(max-width:768px){',
'.sidebar{transform:translateX(-100%);width:240px!important;transition:transform .26s cubic-bezier(.4,0,.2,1),width 0s}',
'.sidebar.mob-open{transform:translateX(0)}',
'.main-area{margin-left:0!important}',
'.menu-toggle{display:flex;align-items:center;justify-content:center}',
'.page-content{padding:12px 10px}',
'.topbar{padding:0 10px;height:48px}',
'.tdiv,.tbc,.cbtn{display:none}',
'}',
].join("");

var ANIMATION_CSS = [
'@keyframes fadeIn{from{opacity:0}to{opacity:1}}',
'@keyframes slideUp{from{opacity:0;transform:translateY(16px)}to{opacity:1;transform:translateY(0)}}',
'@keyframes fadeInUp{from{opacity:0;transform:translateY(20px)}to{opacity:1;transform:translateY(0)}}',
'@keyframes pulse{0%,100%{opacity:1}50%{opacity:.5}}',
'@keyframes spin{to{transform:rotate(360deg)}}',
'@keyframes ticker{0%{transform:translateX(0)}100%{transform:translateX(-50%)}}',
// Card entrance animation
'.card,.stat-card{animation:fadeInUp .3s ease both}',
'.card:nth-child(2),.stat-card:nth-child(2){animation-delay:.05s}',
'.card:nth-child(3),.stat-card:nth-child(3){animation-delay:.1s}',
'.card:nth-child(4),.stat-card:nth-child(4){animation-delay:.15s}',
// Loading spinner
'.spinner{width:32px;height:32px;border:3px solid rgba(92,10,10,.15);border-top-color:#5C0A0A;border-radius:50%;animation:spin .7s linear infinite;margin:0 auto}',
// Skeleton loader
'.skeleton{background:linear-gradient(90deg,rgba(0,0,0,.06) 25%,rgba(0,0,0,.1) 37%,rgba(0,0,0,.06) 63%);background-size:400% 100%;animation:skeleton 1.4s ease infinite}',
'@keyframes skeleton{0%{background-position:100% 50%}100%{background-position:0 50%}}',
].join("");
