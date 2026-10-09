import { expect, test, type Page } from '@playwright/test'

// Personas and fixed exchange GUIDs come from server/Loom.DevSeed (see README "Dev login and personas").
const exchangeUrl = (n: number) => `/exchange/00000000-0000-4000-8000-${String(n).padStart(12, '0')}`

async function loginAs(page: Page, email: string) {
  const response = await page.request.post('/api/auth/dev/login', { data: { email } })
  expect(response.ok(), `dev login as ${email}`).toBeTruthy()
}

test('landing page lists the dev personas', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('button', { name: /admin@loom\.dev/ })).toBeVisible()
})

test('a new user is sent to onboarding', async ({ page }) => {
  await loginAs(page, 'fresh.student@loom.dev')
  await page.goto('/home')
  await expect(page).toHaveURL(/\/onboarding/)
})

test('the coordinator sees their students', async ({ page }) => {
  await loginAs(page, 'ana.coordinator@loom.dev')
  await page.goto('/coordinator')
  await expect(page.getByText('Hana History')).toBeVisible()
})

test("the coordinator opens a student's other exchanges from the row", async ({ page }) => {
  await loginAs(page, 'ana.coordinator@loom.dev')
  await page.goto('/coordinator?q=Mia%20Multi')
  await page.getByRole('button', { name: /\+\s*(2 more|još 2)/ }).click()
  // The switcher lists all three exchanges of the student.
  await expect(page.getByText(/Politecnico di Milano/).first()).toBeVisible()
  await expect(page.getByRole('button', { name: /new exchange|nova razmjena/i }).last()).toBeVisible()
})

test('a coordinator without students sees none of them', async ({ page }) => {
  await loginAs(page, 'ivo.coordinator@loom.dev')
  await page.goto('/coordinator')
  await page.waitForLoadState('networkidle')
  await expect(page.getByText('Hana History')).toHaveCount(0)
})

test('a student opens their approved exchange', async ({ page }) => {
  await loginAs(page, 's.history@loom.dev')
  await page.goto(exchangeUrl(6))
  await expect(page.getByText('IN2259').first()).toBeVisible()
})

test("a student cannot open someone else's exchange", async ({ page }) => {
  await loginAs(page, 's.draft.empty@loom.dev')
  await page.goto(exchangeUrl(6))
  await expect(page).toHaveURL(/\/home$/)
})

test('the access link opens the placeholder exchange without login', async ({ page }) => {
  await page.goto('/access/dev-access-link-13')
  // The token is swapped for a guest session and leaves the address bar.
  await expect(page).toHaveURL(/\/guest\/exchange\/00000000-0000-4000-8000-000000000013$/)
  await expect(page.getByText('IN2064').first()).toBeVisible()
})

test('an unknown access link explains itself', async ({ page }) => {
  await page.goto('/access/not-a-real-link-token')
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
  await expect(page).toHaveURL(/\/access\//)
})

test('a signed-in student is offered to claim the placeholder', async ({ page }) => {
  await loginAs(page, 'claim.student@loom.dev')
  await page.goto('/access/dev-access-link-14')
  await expect(page.getByText('Klara Claim')).toBeVisible()
})

test('starting final recognition warns, then freezes the learning agreement', async ({ page }) => {
  await loginAs(page, 's.approved@loom.dev')
  await page.goto(exchangeUrl(4) + '?tab=recognition')
  await page.getByRole('button', { name: /start final recognition|započni završno priznavanje/i }).click()
  // A confirm dialog warns that this cannot be undone.
  await expect(page.getByRole('dialog')).toBeVisible()
  await page.getByRole('button', { name: /^(start and lock|započni i zaključaj)$/i }).click()
  await expect(page.getByText(/final recognition and grades|konačno priznavanje i ocjene/i)).toBeVisible()
})

test('a student changes the draft, saves it, and the change is kept', async ({ page }) => {
  await loginAs(page, 's.draft.empty@loom.dev')
  const laUrl = `/api/exchanges/${exchangeUrl(1).split('/').pop()}/learning-agreement`
  const entriesBefore = (await (await page.request.get(laUrl)).json()).entries.length

  await page.goto(exchangeUrl(1))
  // Clicking a slot marks it "taken at home" (or clears it); either way the draft changes.
  await page.locator('.la-slot-cell').first().click()
  const save = page.getByRole('button', { name: /^(save|spremi)$/i })
  await expect(save).toBeVisible()
  await Promise.all([
    page.waitForResponse((r) => r.url().endsWith(laUrl) && r.request().method() === 'PUT' && r.ok()),
    save.click(),
  ])
  await expect(save).toBeHidden()

  const entriesAfter = (await (await page.request.get(laUrl)).json()).entries.length
  expect(entriesAfter).not.toBe(entriesBefore)
})

test('a course is placed on a slot by clicking it, then clicking the slot', async ({ page }) => {
  await loginAs(page, 's.draft.message@loom.dev')
  await page.goto(exchangeUrl(3))
  const course = page.locator('[aria-labelledby="la-available-courses"] [draggable="true"]').first()
  await course.click()
  await expect(course).toHaveAttribute('aria-pressed', 'true')
  await page.locator('td[role="button"]').first().click()
  await page.getByRole('dialog').getByRole('button', { name: /^(confirm|potvrdi)$/i }).click()
  await expect(page.getByRole('button', { name: /^(save|spremi)$/i })).toBeVisible()
  await page.getByRole('button', { name: /^(discard|odbaci)/i }).click()
})

test('discarding the draft brings back the saved learning agreement', async ({ page }) => {
  await loginAs(page, 's.draft.empty@loom.dev')
  await page.goto(exchangeUrl(1))
  await page.locator('.la-slot-cell').nth(1).click()
  await page.getByRole('button', { name: /^(discard|odbaci)/i }).click()
  await expect(page.getByRole('button', { name: /^(save|spremi)$/i })).toBeHidden()
})

test('editing the exchange period switches the study semesters with the semester type', async ({ page }) => {
  await loginAs(page, 's.multi@loom.dev')
  await page.goto(exchangeUrl(11))
  await page.locator('[data-menu-anchor] button[aria-haspopup="true"]').click()
  await page.getByRole('button', { name: /^(edit|uredi)$/i }).click()
  const dialog = page.getByRole('dialog')
  await dialog.getByRole('radio', { name: /^(summer|ljetni)$/i }).click()
  await expect(dialog.getByRole('button', { name: '4', exact: true })).toBeVisible()
  await dialog.getByRole('button', { name: '2', exact: true }).click()
  await expect(dialog.getByRole('button', { name: '2', exact: true })).toHaveAttribute('aria-pressed', 'true')
  await dialog.getByRole('button', { name: /^(save|spremi)$/i }).click()
  await expect(dialog).toBeHidden()
  await expect(page.getByText(/(Summer|Ljetni) \(2\)/)).toBeVisible()
})

test('the learning agreement history lists numbered versions', async ({ page }) => {
  await loginAs(page, 's.history@loom.dev')
  await page.goto(exchangeUrl(6))
  await page.getByRole('button', { name: /^(history|povijest)$/i }).click()
  await expect(page.getByText(/(version|verzija) 3/i)).toBeVisible()
})

test('the official document is generated by the server', async ({ page }) => {
  await loginAs(page, 's.recognition.done@loom.dev')
  await page.goto(exchangeUrl(8))
  const [download] = await Promise.all([
    page.waitForEvent('download'),
    page.getByRole('button', { name: /official document|službeni dokument/i }).first().click(),
  ])
  expect(download.suggestedFilename()).toMatch(/\.xlsx$/)
})

test('the coordinator list follows the filters in the address bar', async ({ page }) => {
  await loginAs(page, 'ana.coordinator@loom.dev')
  const filters = await (await page.request.get('/api/coordinator/students/filters')).json()
  const institution: string = filters.partnerInstitutions[0]
  await page.goto(`/coordinator?partnerInstitution=${encodeURIComponent(institution)}`)
  // Every student row shows an exchange at that institution.
  await expect(page.getByText(institution).first()).toBeVisible()
  const others = (filters.partnerInstitutions as string[]).filter((name) => name !== institution)
  for (const other of others) await expect(page.getByText(other, { exact: true })).toHaveCount(0)
})

test('the admin pages the coordinator whitelist and the partner institutions', async ({ page }) => {
  await loginAs(page, 'admin@loom.dev')
  await page.goto('/admin')
  await expect(page.getByRole('heading', { name: /coordinator whitelist|whitelist koordinatora/i })).toBeVisible()
  await page.goto('/admin?tab=institutions')
  await expect(page.getByRole('heading', { name: /partner institutions|partnerske institucije/i })).toBeVisible()
  await expect(page.getByRole('alert')).toHaveCount(0)
})

test('the admin panel lists users', async ({ page }) => {
  await loginAs(page, 'admin@loom.dev')
  await page.goto('/admin')
  await expect(page.getByText('ana.coordinator@loom.dev').first()).toBeVisible()
})
