import { chromium } from 'playwright';
import { writeFileSync, mkdirSync } from 'fs';

const BASE  = 'http://localhost:3000';
const SHOTS = 'D:/projects/StudentPortal/web_screenshots';
mkdirSync(SHOTS, { recursive: true });

const results = [];
let sc = 0;

function log(status, name, detail = '') {
  console.log(`${status.padEnd(5)} ${name}${detail ? ' — ' + detail : ''}`);
  results.push({ status, name, detail });
}

async function shot(page, name) {
  const file = `${SHOTS}/${String(++sc).padStart(2,'0')}_${name.replace(/[^a-zA-Z0-9]/g,'_')}.png`;
  await page.screenshot({ path: file, fullPage: false }).catch(() => {});
}

async function loginAndTest(browser, roleName, username, password, pages) {
  const ctx  = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();

  // Navigate to app — will redirect to Keycloak login
  try {
    await page.goto(BASE, { waitUntil: 'domcontentloaded', timeout: 15000 });
    await page.waitForTimeout(1500);
  } catch (e) {
    log('FAIL', `${roleName} — initial navigation`, e.message.slice(0, 80));
    await ctx.close();
    return;
  }

  // Fill login form (custom React login at /login)
  try {
    await page.waitForURL(u => String(u).includes('/login'), { timeout: 10000 });
    // inputs have no id/name — select by placeholder
    await page.locator('input[placeholder="Enter your username"]').waitFor({ timeout: 10000 });
    await page.fill('input[placeholder="Enter your username"]', username);
    await page.fill('input[placeholder="Enter your password"]', password);
    await page.click('button[type="submit"]');
    await page.waitForURL(url => !String(url).includes('/login'), { timeout: 15000 });
    await page.waitForTimeout(1500);
    await shot(page, `${roleName}_after_login`);
    log('PASS', `${roleName} login`, `landed at ${page.url()}`);
  } catch (e) {
    await shot(page, `${roleName}_login_fail`);
    log('FAIL', `${roleName} login`, e.message.slice(0, 100));
    await ctx.close();
    return;
  }

  // Test each page for this role
  for (const [label, path, keyword] of pages) {
    try {
      await page.goto(`${BASE}${path}`, { waitUntil: 'domcontentloaded', timeout: 12000 });
      await page.waitForTimeout(1200);
      const body = (await page.textContent('body').catch(() => '')).toLowerCase();
      const ok   = !keyword || body.includes(keyword.toLowerCase());
      // Check page didn't redirect back to login
      const notRedirected = !page.url().includes('login') && !page.url().includes('8080');
      await shot(page, `${roleName}_${label}`);
      if (!notRedirected) {
        log('FAIL', `${roleName} — ${label}`, 'redirected to login (unauthorized)');
      } else if (!ok) {
        log('WARN', `${roleName} — ${label}`, `"${keyword}" not found`);
      } else {
        log('PASS', `${roleName} — ${label}`);
      }
    } catch (e) {
      log('FAIL', `${roleName} — ${label}`, e.message.slice(0, 80));
    }
  }

  await ctx.close();
}

const browser = await chromium.launch({ headless: true });

console.log('\n=== WEB UI TEST — localhost:3000 ===\n');

// ── Home page ───────────────────────────────────────────────────
console.log('── HOME ──');
{
  const ctx  = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();
  try {
    const r = await page.goto(BASE, { waitUntil: 'domcontentloaded', timeout: 15000 });
    await page.waitForTimeout(1000);
    await shot(page, '00_home');
    log('PASS', 'Home page loads', `HTTP ${r.status()} → ${page.url()}`);
  } catch (e) {
    log('FAIL', 'Home page loads', e.message.slice(0, 80));
  }
  await ctx.close();
}

// ── Student ──────────────────────────────────────────────────────
console.log('\n── STUDENT FLOW ──');
await loginAndTest(browser, 'Student', 'student.one', 'hazem123', [
  ['Dashboard',     '/',                      null],
  ['Profile',       '/student/profile',       null],
  ['Results',       '/student/results',       null],
  ['Schedule',      '/student/schedule',      null],
  ['Register',      '/student/register',      null],
  ['Notifications', '/student/notifications', null],
  ['Assignments',   '/student/assignments',   null],
  ['Chat',          '/chat',                  null],
]);

// ── Instructor ────────────────────────────────────────────────────
console.log('\n── INSTRUCTOR FLOW ──');
await loginAndTest(browser, 'Instructor', 'instructor.one', 'Instructor@123', [
  ['Sections',    '/instructor/sections',    null],
  ['Attendance',  '/instructor/attendance',  null],
  ['Grading',     '/instructor/grading',     null],
  ['Assignments', '/instructor/assignments', null],
]);

// ── Admin ─────────────────────────────────────────────────────────
console.log('\n── ADMIN FLOW ──');
await loginAndTest(browser, 'Admin', 'admin.portal', 'Admin@123', [
  ['Students',    '/admin/students',    null],
  ['Instructors', '/admin/instructors', null],
  ['Sections',    '/admin/sections',    null],
  ['Courses',     '/admin/courses',     null],
  ['Policies',    '/admin/policies',    null],
  ['Reports',     '/admin/reports',     null],
  ['Import',      '/admin/import',      null],
]);

await browser.close();

// ── Summary ────────────────────────────────────────────────────────
const pass = results.filter(r => r.status === 'PASS').length;
const warn = results.filter(r => r.status === 'WARN').length;
const fail = results.filter(r => r.status === 'FAIL').length;

console.log(`\n=== RESULTS: ${pass} PASS  ${warn} WARN  ${fail} FAIL  (${results.length} total) ===`);
console.log(`Screenshots → ${SHOTS}\n`);
console.log('Detail:');
results.filter(r => r.status !== 'PASS').forEach(r => console.log(`  ${r.status} ${r.name}${r.detail ? ' — ' + r.detail : ''}`));

writeFileSync('D:/projects/StudentPortal/web_ui_results.json',
  JSON.stringify({ timestamp: new Date().toISOString(), pass, warn, fail, total: results.length, results, screenshots: SHOTS }, null, 2));
