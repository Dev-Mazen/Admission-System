const toNumber = (value) => {
  const number = Number(value);
  return Number.isFinite(number) ? number : 0;
};

export const hasIqExamScore = (student) =>
  student?.examIqScore !== null &&
  student?.examIqScore !== undefined;

export const getExamTotal = (student) => {
  if (student?.examTotal !== null && student?.examTotal !== undefined) {
    return toNumber(student.examTotal);
  }

  return (
    toNumber(student?.examMathScore) +
    toNumber(student?.examEnglishScore) +
    toNumber(student?.examArabicScore) +
    toNumber(student?.examSoftwareScore) +
    toNumber(student?.examIqScore)
  );
};

export const getExamMaximum = (student) =>
  toNumber(student?.examMaxScore) || (hasIqExamScore(student) ? 100 : 60);

export const getExamPercentage = (student) => {
  if (
    student?.examPercentage !== null &&
    student?.examPercentage !== undefined
  ) {
    return toNumber(student.examPercentage);
  }

  return (getExamTotal(student) * 100) / getExamMaximum(student);
};

export const getExamAdmissionContribution = (student) =>
  (getExamPercentage(student) * 60) / 100;

export const getExamSectionMaximum = (student, sectionName) => {
  const configuredMaximums = {
    Arabic: student?.examArabicMaxScore,
    English: student?.examEnglishMaxScore,
    Math: student?.examMathMaxScore,
    Software: student?.examSoftwareMaxScore,
    IQ: student?.examIqMaxScore,
  };
  if (
    configuredMaximums[sectionName] !== null &&
    configuredMaximums[sectionName] !== undefined
  ) {
    return toNumber(configuredMaximums[sectionName]);
  }

  if (!hasIqExamScore(student)) return 15;

  const maximums = {
    Arabic: 15,
    English: 30,
    Math: 30,
    Software: 15,
    IQ: 10,
  };

  return maximums[sectionName] ?? 0;
};
