const assert = require('node:assert/strict');
const test = require('node:test');
const Autosave = require('../../QLThiTN/QLThiTN.Web/wwwroot/js/exam-autosave.js');

test('network failure retains unsaved changes and retries successfully', async () => {
    let fail = true, calls = 0;
    const save = new Autosave(async () => {
        calls++;
        if (fail) throw new Error('offline');
        return { success: true };
    }, () => {});
    save.markDirty();
    assert.equal(await save.flush(), false);
    assert.equal(save.dirty, true);
    fail = false;
    assert.equal(await save.flush(), true);
    assert.equal(save.dirty, false);
    assert.equal(calls, 2);
});

test('application rejection does not acknowledge answers', async () => {
    const save = new Autosave(async () => ({ success: false, message: 'expired' }), () => {});
    save.markDirty();
    assert.equal(await save.flush(), false);
    assert.equal(save.dirty, true);
});

test('changes made while saving remain dirty; only one request is in flight', async () => {
    let finish, calls = 0;
    const save = new Autosave(() => { calls++; return new Promise(resolve => { finish = resolve; }); }, () => {});
    save.markDirty();
    const pending = save.flush();
    await Promise.resolve();
    save.markDirty();
    assert.equal(save.flush(), pending);
    finish({ success: true });
    await pending;
    assert.equal(calls, 1);
    assert.equal(save.dirty, true);
    const next = save.flush();
    await Promise.resolve();
    finish({ success: true });
    await next;
    assert.equal(save.dirty, false);
    assert.equal(calls, 2);
});
