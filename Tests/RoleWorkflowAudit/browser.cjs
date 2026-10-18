const { chromium } = require('playwright');
const fs = require('node:fs');
const path = require('node:path');
const [base, output, examId] = process.argv.slice(2);
(async () => {
    const browser = await chromium.launch({ channel: 'msedge', headless: true });
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    const page = await context.newPage();
    const results = [], errors = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('dialog', dialog => dialog.accept());
    async function screenshotSafe(targetPage, filePath) {
        for (let attempt = 0; attempt < 5; attempt++) {
            try {
                await targetPage.screenshot({ path: filePath, fullPage: true });
                return;
            } catch (err) {
                if (attempt === 4) throw err;
                await new Promise(r => setTimeout(r, 250));
            }
        }
    }
    async function capture(name, route) {
        const response = await page.goto(base + route, { waitUntil: 'networkidle' });
        await screenshotSafe(page, path.join(output, name + '.png'));
        const details = await page.evaluate(() => ({
            overflow: document.documentElement.scrollWidth > innerWidth,
            unlabelled: [...document.querySelectorAll('input:not([type=hidden]):not([type=submit]):not([type=button]),select,textarea')]
                .filter(x => !x.labels?.length && !x.getAttribute('aria-label') && !x.getAttribute('aria-labelledby')).map(x => x.name || x.id),
            brokenImages: [...document.images].filter(x => x.complete && !x.naturalWidth).map(x => x.getAttribute('src'))
        }));
        results.push({ page: name, status: response.status(), ...details });
    }
    try {
        await capture('web-home-desktop', '');
        await capture('web-register-desktop', 'Account/Register');
        await capture('web-login-desktop', 'Account/Login');
        await page.locator('#Username').fill('student-a');
        await page.locator('#Password').fill('Test-password-123!');
        await Promise.all([page.waitForURL(base), page.locator('button[type=submit]').click()]);
        await capture('web-profile-desktop', 'Account/Profile');
        await capture('web-schedule-desktop', 'Exam/Schedule');
        await capture('web-library-desktop', 'Exam/KhoDe');
        await capture('web-history-desktop', 'Exam/History');
        await capture('web-learning-desktop', 'Exam/LearningSchedule');
        await capture('web-taking-desktop', 'Exam/Taking/' + examId);
        results.push({ test: 'Stored HTML execution', executed: await page.evaluate(() => window.__auditXss === true) });
        results.push({ test: 'Timer initialized', text: await page.locator('#timer').textContent() });
        const options = page.locator('.option-item');
        if (await options.count()) {
            await options.first().click();
            await page.waitForTimeout(1800);
            results.push({ test: 'Autosave online', text: await page.locator('#autosaveStatus').textContent() });
            await context.setOffline(true);
            await options.last().click();
            await page.waitForTimeout(1800);
            results.push({ test: 'Autosave offline', text: await page.locator('#autosaveStatus').textContent() });
            await context.setOffline(false);
            await page.waitForTimeout(1800);
            results.push({ test: 'Autosave recovery', text: await page.locator('#autosaveStatus').textContent() });
        }
        await page.setViewportSize({ width: 390, height: 844 });
        await screenshotSafe(page, path.join(output, 'web-taking-mobile.png'));
        results.push({ page: 'web-taking-mobile', overflow: await page.evaluate(() => document.documentElement.scrollWidth > innerWidth) });
        await capture('web-library-mobile', 'Exam/KhoDe');
        await capture('web-profile-mobile', 'Account/Profile');
        await page.goto(base + 'Account/Logout');
        await page.goto(base + 'Account/Login');
        await page.locator('#Username').fill('teacher');
        await page.locator('#Password').fill('Test-password-123!');
        await Promise.all([page.waitForURL(base), page.locator('button[type=submit]').click()]);
        await capture('web-teacher-home', '');
        results.push({ test: 'Teacher web login', url: page.url(), hasStudentSchedule: await page.getByRole('link', { name: 'Lịch thi', exact: true }).count() });
    } catch (error) {
        results.push({ fatal: error.message });
        process.exitCode = 1;
    } finally {
        const outPath = path.join(output, 'browser-results.json');
        try {
            fs.writeFileSync(outPath, JSON.stringify({ results, pageErrors: errors }, null, 2));
        } catch (e) {
            try {
                fs.writeFileSync(outPath + '.tmp', JSON.stringify({ results, pageErrors: errors }, null, 2));
                fs.renameSync(outPath + '.tmp', outPath);
            } catch (err2) {
                console.error('Could not write browser-results.json:', err2.message);
            }
        }
        console.log(JSON.stringify({ results, pageErrors: errors }, null, 2));
        await browser.close();
    }
})();
