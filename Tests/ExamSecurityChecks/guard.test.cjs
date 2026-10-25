const assert = require('node:assert/strict');
const Guard = require('../../QLThiTN/QLThiTN.Web/wwwroot/js/exam-guard.js');
(async () => {
    let submitting = false, navigated = false, notices = 0, saves = 0;
    let confirmations = [false];
    const events = [];
    const guard = new Guard({ isSubmitting: () => submitting, save: async () => { saves++; return true; },
        record: type => events.push(type), notice: () => notices++,
        confirm: () => confirmations.shift(), navigate: () => { navigated = true; } });
    let warned = false;
    guard.beforeUnload({ preventDefault: () => { warned = true; } });
    assert(warned, 'warn even when all answers are saved');
    await guard.navigate('/away');
    assert(!navigated && saves === 0, 'cancel stays on exam');
    guard.visibility(true); guard.visibility(true); guard.visibility(false);
    assert.deepEqual(events, ['hidden', 'visible']);
    assert.equal(notices, 1);
    confirmations = [true, false];
    guard.options.save = async () => false;
    await guard.navigate('/away');
    assert(!navigated, 'failed save requires separate confirmation');
    submitting = true; warned = false;
    guard.beforeUnload({ preventDefault: () => { warned = true; } });
    assert(!warned, 'normal submission is not blocked');
    submitting = false; confirmations = [true]; guard.options.save = async () => true;
    await guard.navigate('/away');
    assert(navigated && guard.leaving);
    console.log('PASS: leave warnings, cancellation, visibility, failed save, submission, navigation');
})().catch(e => { console.error(e); process.exitCode = 1; });
