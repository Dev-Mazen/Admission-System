export function getAgeParts(dateOfBirth, referenceDate = new Date('2026-10-01T00:00:00Z')) {
  if (!dateOfBirth) return { years: 0, months: 0, days: 0 };

  const parsed = new Date(dateOfBirth);
  if (Number.isNaN(parsed.getTime())) return { years: 0, months: 0, days: 0 };

  const targetDate = new Date(referenceDate);

  let years = targetDate.getFullYear() - parsed.getFullYear();
  let months = targetDate.getMonth() - parsed.getMonth();
  let days = targetDate.getDate() - parsed.getDate();

  if (days < 0) {
    const previousMonth = new Date(targetDate.getFullYear(), targetDate.getMonth(), 0);
    days += previousMonth.getDate();
    months -= 1;
  }

  if (months < 0) {
    years -= 1;
    months += 12;
  }

  if (years < 0) {
    years = 0;
    months = 0;
    days = 0;
  }

  return { years, months, days };
}

export function calculateStudentAge(dateOfBirth, referenceDate = new Date('2026-10-01T00:00:00Z')) {
  if (!dateOfBirth) return null;

  const { days, months, years } = getAgeParts(dateOfBirth, referenceDate);
  return `${days} day${days === 1 ? '' : 's'} ${months} month${months === 1 ? '' : 's'} ${years} year${years === 1 ? '' : 's'}`;
}
