import { useState } from "react";

/**
 * ReportConfigModal — Per-Print Report Options Selection Modal
 * Allows SuperAdmin to pick which report sections/fields to include before generating the PDF.
 */
export default function ReportConfigModal({ isOpen, onClose, onGenerate, studentName }) {
  const [options, setOptions] = useState({
    showApplicantInfo: true,
    showStudentCode: true,
    showFinalScore: true,
    showFactorBreakdown: true,
    showPreparatoryGrades: true,
    showSchoolExamBreakdown: true,
    showInterviewBreakdown: true,
    showCalculationFormula: true,
  });

  if (!isOpen) return null;

  const toggleOption = (key) => {
    setOptions((prev) => ({ ...prev, [key]: !prev[key] }));
  };

  const handleSelectAll = (val) => {
    setOptions({
      showApplicantInfo: val,
      showStudentCode: val,
      showFinalScore: val,
      showFactorBreakdown: val,
      showPreparatoryGrades: val,
      showSchoolExamBreakdown: val,
      showInterviewBreakdown: val,
      showCalculationFormula: val,
    });
  };

  const handleConfirm = () => {
    onGenerate(options);
    onClose();
  };

  const sectionsList = [
    { key: "showApplicantInfo", label: "Applicant Information Block", desc: "Full Name, National ID, Application ID, City" },
    { key: "showStudentCode", label: "Student Code Field", desc: "Displays Student Code in Applicant Info if present" },
    { key: "showFinalScore", label: "Final Admission Score Card", desc: "Prominent percentage card with status badge" },
    { key: "showFactorBreakdown", label: "Overall Factor Breakdown Table", desc: "School Exam, Interview, Prep Cert, Ministry weights" },
    { key: "showPreparatoryGrades", label: "Preparatory Certificate Grades Table", desc: "Grade 9 Mathematics, English, and Total score" },
    { key: "showSchoolExamBreakdown", label: "School Admission Exam Subject Breakdown", desc: "Subject scores: Math, English, Arabic, Software, IQ" },
    { key: "showInterviewBreakdown", label: "Interviewer Evaluation Breakdown", desc: "Individual panel interviewer score breakdown" },
    { key: "showCalculationFormula", label: "Audit Calculation Formula Box", desc: "Transparent formula string & snapshot metadata" },
  ];

  return (
    <div className="fixed inset-0 backdrop-blur-sm bg-gray-900 bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl max-w-lg w-full shadow-2xl overflow-hidden border border-gray-100 animate-in fade-in zoom-in-95 duration-150">
        {/* Header */}
        <div className="bg-gradient-to-r from-red-600 to-red-700 px-6 py-4 flex items-center justify-between text-white">
          <div className="flex items-center space-x-3 rtl:space-x-reverse">
            <div className="w-9 h-9 bg-white bg-opacity-20 rounded-lg flex items-center justify-center font-bold text-lg">
              📄
            </div>
            <div>
              <h3 className="font-bold text-lg leading-tight">Customize PDF Report</h3>
              {studentName && <p className="text-xs text-red-100 opacity-90">{studentName}</p>}
            </div>
          </div>
          <button
            onClick={onClose}
            className="text-red-100 hover:text-white transition-colors p-1 rounded-lg hover:bg-white/10"
          >
            ✕
          </button>
        </div>

        {/* Body */}
        <div className="p-6 space-y-4 max-h-[70vh] overflow-y-auto">
          <div className="flex items-center justify-between pb-2 border-b border-gray-100 text-xs font-semibold text-gray-500">
            <span>Select Sections to Include in PDF</span>
            <div className="space-x-2 rtl:space-x-reverse">
              <button
                type="button"
                onClick={() => handleSelectAll(true)}
                className="text-red-600 hover:underline font-medium"
              >
                Select All
              </button>
              <span>•</span>
              <button
                type="button"
                onClick={() => handleSelectAll(false)}
                className="text-gray-600 hover:underline font-medium"
              >
                Clear All
              </button>
            </div>
          </div>

          <div className="space-y-3">
            {sectionsList.map((sec) => (
              <label
                key={sec.key}
                className={`flex items-start p-3 rounded-lg border cursor-pointer transition-all ${
                  options[sec.key]
                    ? "border-red-200 bg-red-50/50 shadow-sm"
                    : "border-gray-200 bg-gray-50/40 hover:bg-gray-50 opacity-75"
                }`}
              >
                <input
                  type="checkbox"
                  checked={options[sec.key]}
                  onChange={() => toggleOption(sec.key)}
                  className="mt-0.5 h-4 w-4 rounded border-gray-300 text-red-600 focus:ring-red-500 transition cursor-pointer"
                />
                <div className="ml-3 rtl:mr-3 text-sm">
                  <span className="font-semibold text-gray-800 block">{sec.label}</span>
                  <span className="text-xs text-gray-500 leading-normal">{sec.desc}</span>
                </div>
              </label>
            ))}
          </div>
        </div>

        {/* Footer */}
        <div className="bg-gray-50 px-6 py-4 border-t border-gray-100 flex items-center justify-end space-x-3 rtl:space-x-reverse">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition shadow-sm"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={handleConfirm}
            className="px-5 py-2 text-sm font-semibold text-white bg-red-600 hover:bg-red-700 active:bg-red-800 rounded-lg shadow-md hover:shadow-lg transition-all flex items-center space-x-2 rtl:space-x-reverse"
          >
            <span>📄 Generate & Open PDF</span>
          </button>
        </div>
      </div>
    </div>
  );
}
