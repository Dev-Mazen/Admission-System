import assert from 'node:assert/strict';
import test from 'node:test';
import { calculateStudentAge } from './studentProfile.js';

const referenceDate = new Date('2026-10-01T00:00:00Z');

test('calculateStudentAge returns days, months and years on 2026-10-01', () => {
  assert.equal(calculateStudentAge('2009-06-15', referenceDate), '16 days 3 months 17 years');
  assert.equal(calculateStudentAge('2015-01-01', referenceDate), '0 days 9 months 11 years');
  assert.equal(calculateStudentAge('', referenceDate), null);
  assert.equal(calculateStudentAge(null, referenceDate), null);
});
