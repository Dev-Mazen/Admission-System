/**
 * @fileoverview Column definitions, cell renderers, formatters, and utility
 * functions for the StudentGrid. All pure functions — no React state.
 */

import React from 'react';
import { STATUS_MAP } from './studentGrid.types.js';
import { adminAPI } from '../../utils/api.js';

// ─── Formatters ───────────────────────────────────────────────────────────────

/** @param {string|undefined} phone @returns {string} */
export function buildWaUrl(phone) {
  if (!phone) return '';
  const normalized = phone.startsWith('+') ? phone : `+20${phone}`;
  return `https://wa.me/${normalized.replace(/\s/g, '')}`;
}

/**
 * Compute exam total from individual scores.
 * @param {import('./studentGrid.types.js').StudentRow} s
 * @param {((s: any) => number)|undefined} externalFn
 */
export function computeExamTotal(s, externalFn) {
  if (externalFn) return externalFn(s);
  return (s.examArabicScore ?? 0) + (s.examEnglishScore ?? 0) +
         (s.examMathScore ?? 0) + (s.examSoftwareScore ?? 0);
}

/**
 * Compute exam maximum.
 * @param {import('./studentGrid.types.js').StudentRow} s
 * @param {((s: any) => number)|undefined} externalFn
 */
export function computeExamMax(s, externalFn) {
  if (externalFn) return externalFn(s);
  return 60;
}

// ─── Cell Renderers ───────────────────────────────────────────────────────────

/** Monospace national-ID cell */
export function NationalIdCell({ value }) {
  return (
    <span style={{
      fontFamily: "'JetBrains Mono', 'Fira Code', monospace",
      fontSize: 11,
      color: 'var(--sg-muted)',
      letterSpacing: '0.03em',
    }}>
      {value}
    </span>
  );
}

/** Student name with subtle avatar initial */
export function NameCell({ value }) {
  if (!value) return null;
  const initials = value.trim().split(/\s+/).slice(0, 2).map(w => w[0]).join('');
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 10, height: '100%' }}>
      <div style={{
        width: 32, height: 32, borderRadius: 8,
        background: 'var(--sg-avatar-bg)',
        color: 'var(--sg-avatar-color)',
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        fontSize: 11, fontWeight: 700, flexShrink: 0,
        fontFamily: 'system-ui',
      }}>
        {initials}
      </div>
      <span style={{ fontWeight: 500, fontSize: 13, color: 'var(--sg-text)' }}>{value}</span>
    </div>
  );
}

/** Exam score bar cell: total / max with mini progress bar */
export function ExamScoreCell({ data, getExamTotal, getExamMaximum }) {
  if (!data) return null;
  const total = computeExamTotal(data, getExamTotal);
  const max   = computeExamMax(data, getExamMaximum);
  const pct   = max > 0 ? (total / max) * 100 : 0;
  const color = pct >= 70 ? '#10b981' : pct >= 45 ? '#f59e0b' : '#ef4444';
  return (
    <div style={{ display: 'flex', flexDirection: 'column', justifyContent: 'center', gap: 4, height: '100%', padding: '6px 0' }}>
      <span style={{ fontSize: 12, fontWeight: 600, color: 'var(--sg-text)' }}>{total} / {max}</span>
      <div style={{ height: 3, borderRadius: 99, background: 'var(--sg-track)', width: 70, overflow: 'hidden' }}>
        <div style={{ width: `${pct}%`, height: '100%', background: color, borderRadius: 99, transition: 'width 0.4s ease' }} />
      </div>
    </div>
  );
}

/** Total percentage pill */
export function TotalPctCell({ data, calculatePercentage }) {
  if (!data) return null;
  const pct = calculatePercentage ? calculatePercentage(data) : (data.totalPercentage ?? 0);
  const color = pct >= 70 ? '#10b981' : pct >= 45 ? '#f59e0b' : '#ef4444';
  return (
    <span style={{
      fontWeight: 700, fontSize: 13, color,
      background: `${color}14`,
      padding: '2px 10px', borderRadius: 99,
      border: `1px solid ${color}30`,
    }}>
      {pct}%
    </span>
  );
}

/** Boolean badge: Yes / No */
export function BoolCell({ value }) {
  return value
    ? <span style={{ color: '#10b981', fontWeight: 600, fontSize: 12 }}>✓</span>
    : <span style={{ color: 'var(--sg-muted)', fontWeight: 400, fontSize: 12 }}>—</span>;
}

/** Status badge cell renderer */
export function StatusCell({ data }) {
  const status = Number(data?.status ?? data?.Status ?? 1);
  const cfg = STATUS_MAP[status] || STATUS_MAP[1];

  return (
    <div style={{ display: 'flex', alignItems: 'center', height: '100%' }}>
      <span
        style={{
          background: cfg.bg,
          color: cfg.color,
          border: `1px solid ${cfg.border}`,
          borderRadius: 999,
          padding: '2px 10px',
          fontSize: 11,
          fontWeight: 700,
          lineHeight: 1.4,
          display: 'inline-flex',
          alignItems: 'center',
          gap: 5,
        }}
      >
        <span style={{ width: 6, height: 6, borderRadius: '50%', background: cfg.dot }} />
        {cfg.label}
      </span>
    </div>
  );
}

/** WhatsApp contact buttons */
export function ContactsCell({ data }) {
  if (!data) return null;
  const open = (phone) => {
    const url = buildWaUrl(phone);
    if (url) window.open(url, '_blank', 'noopener');
  };
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 6, height: '100%' }}>
      <button
        onClick={(e) => { e.stopPropagation(); open(data.phoneNumber); }}
        disabled={!data.phoneNumber}
        style={{
          fontSize: 11, fontWeight: 600, color: '#10b981',
          background: 'rgba(16,185,129,0.08)', border: '1px solid rgba(16,185,129,0.25)',
          borderRadius: 6, padding: '2px 8px', cursor: data.phoneNumber ? 'pointer' : 'default',
          opacity: data.phoneNumber ? 1 : 0.35, transition: 'background 0.15s',
        }}
        onMouseEnter={e => { if (data.phoneNumber) e.currentTarget.style.background = 'rgba(16,185,129,0.16)'; }}
        onMouseLeave={e => { e.currentTarget.style.background = 'rgba(16,185,129,0.08)'; }}
      >
        Student
      </button>
      <button
        onClick={(e) => { e.stopPropagation(); open(data.parentPhoneNumber); }}
        disabled={!data.parentPhoneNumber}
        style={{
          fontSize: 11, fontWeight: 600, color: '#6366f1',
          background: 'rgba(99,102,241,0.08)', border: '1px solid rgba(99,102,241,0.25)',
          borderRadius: 6, padding: '2px 8px', cursor: data.parentPhoneNumber ? 'pointer' : 'default',
          opacity: data.parentPhoneNumber ? 1 : 0.35, transition: 'background 0.15s',
        }}
        onMouseEnter={e => { if (data.parentPhoneNumber) e.currentTarget.style.background = 'rgba(99,102,241,0.16)'; }}
        onMouseLeave={e => { e.currentTarget.style.background = 'rgba(99,102,241,0.08)'; }}
      >
        Parent
      </button>
    </div>
  );
}

/**
 * Safely extract score for a given interviewer index (0, 1, 2).
 * @param {import('./studentGrid.types.js').StudentRow} s
 * @param {number} idx - 0 for Interviewer 1, 1 for Interviewer 2, 2 for Interviewer 3
 */
export function getInterviewerScore(s, idx) {
  if (!s) return null;

  // 1. Array of interviewScores
  if (Array.isArray(s.interviewScores) && s.interviewScores.length > idx) {
    const item = s.interviewScores[idx];
    if (typeof item === 'number') return item;
    if (item && typeof item === 'object') {
      const val = item.score ?? item.totalScore ?? item.value ?? item.interviewScore;
      if (val != null && !isNaN(Number(val))) return Number(val);
    }
  }

  // 2. Direct properties on student object
  const keys = [
    `interviewer${idx + 1}Score`,
    `interviewer${idx + 1}`,
    `interviewerScore${idx + 1}`,
    `interviewScore${idx + 1}`,
  ];
  for (const key of keys) {
    if (s[key] != null && !isNaN(Number(s[key]))) {
      return Number(s[key]);
    }
  }

  // 3. Fallback: if student has single interviewScore, show it for Interviewer 1
  if (idx === 0) {
    const single = s.interviewScore ?? s.InterviewScore;
    if (single != null && !isNaN(Number(single))) return Number(single);
  }

  return null;
}

// ─── Column Definitions ───────────────────────────────────────────────────────

/**
 * Build AG Grid column definitions.
 * Wrapped in useMemo in the parent to avoid re-creation on every render.
 *
 * @param {{ calculatePercentage?: Function; getExamTotal?: Function; getExamMaximum?: Function }} opts
 * @returns {import('ag-grid-community').ColDef[]}
 */
export function buildColumnDefs({ calculatePercentage, getExamTotal, getExamMaximum } = {}) {
  return [
    // Checkbox selection column next to National ID — pinned left
    {
      colId:          '__check__',
      headerName:     '',
      checkboxSelection: true,
      headerCheckboxSelection: true,
      pinned:         'left',
      width:          44,
      minWidth:       44,
      maxWidth:       44,
      resizable:      false,
      sortable:       false,
      filter:         false,
      suppressMovable: true,
    },

    {
      colId:        'nationalId',
      field:        'nationalId',
      headerName:   'National ID',
      width:        150,
      filter:       'agTextColumnFilter',
      cellRenderer: NationalIdCell,
    },

    {
      colId:        'fullName',
      field:        'fullName',
      headerName:   'Student Name',
      flex:         1.8,
      minWidth:     200,
      filter:       'agTextColumnFilter',
      cellRenderer: NameCell,
    },

    {
      colId:        'city',
      field:        'city',
      headerName:   'City',
      width:        120,
      filter:       'agTextColumnFilter',
    },

    {
      colId:         'examScore',
      headerName:    'Exam Score',
      width:         140,
      sortable:      true,
      filter:        'agNumberColumnFilter',
      valueGetter:   (p) => p.data ? computeExamTotal(p.data, getExamTotal) : 0,
      cellRenderer:  (p) => (
        <ExamScoreCell
          data={p.data}
          getExamTotal={getExamTotal}
          getExamMaximum={getExamMaximum}
        />
      ),
    },

    {
      colId:         'ministryExamPercentage',
      field:         'ministryExamPercentage',
      headerName:    'Ministry %',
      width:         120,
      filter:        'agNumberColumnFilter',
      sortable:      true,
      valueFormatter: (p) => p.value != null ? `${p.value}%` : '—',
    },

    {
      colId:         'interviewer1',
      headerName:    'Interviewer 1',
      width:         130,
      sortable:      true,
      filter:        'agNumberColumnFilter',
      valueGetter:   (p) => getInterviewerScore(p.data, 0),
      valueFormatter: (p) => p.value != null ? Number(p.value).toFixed(1) : '—',
    },

    {
      colId:         'interviewer2',
      headerName:    'Interviewer 2',
      width:         130,
      sortable:      true,
      filter:        'agNumberColumnFilter',
      valueGetter:   (p) => getInterviewerScore(p.data, 1),
      valueFormatter: (p) => p.value != null ? Number(p.value).toFixed(1) : '—',
    },

    {
      colId:         'interviewer3',
      headerName:    'Interviewer 3',
      width:         130,
      sortable:      true,
      filter:        'agNumberColumnFilter',
      valueGetter:   (p) => getInterviewerScore(p.data, 2),
      valueFormatter: (p) => p.value != null ? Number(p.value).toFixed(1) : '—',
    },

    {
      colId:         'interviewScore',
      field:         'interviewScore',
      headerName:    'Interview Total',
      width:         130,
      sortable:      true,
      filter:        'agNumberColumnFilter',
      valueFormatter: (p) => p.value != null ? Number(p.value).toFixed(1) : '—',
    },

    {
      colId:         'totalPct',
      headerName:    'Total %',
      width:         110,
      sortable:      true,
      filter:        'agNumberColumnFilter',
      valueGetter:   (p) => p.data ? (calculatePercentage ? calculatePercentage(p.data) : (p.data.totalPercentage ?? 0)) : 0,
      cellRenderer:  (p) => (
        <TotalPctCell data={p.data} calculatePercentage={calculatePercentage} />
      ),
    },

    {
      colId:         'hasOnlineTrainingCourses',
      field:         'hasOnlineTrainingCourses',
      headerName:    'Online Training',
      width:         140,
      filter:        'agSetColumnFilter',
      filterParams:  { values: [true, false] },
      cellRenderer:  (p) => <BoolCell value={p.value} />,
    },

    {
      colId:        'hasICDLLicense',
      field:        'hasICDLLicense',
      headerName:   'ICDL',
      width:        80,
      filter:       'agSetColumnFilter',
      filterParams: { values: [true, false] },
      cellRenderer: (p) => <BoolCell value={p.value} />,
    },

    {
      colId:        'hasLaptop',
      field:        'hasLaptop',
      headerName:   'Laptop',
      width:        90,
      filter:       'agSetColumnFilter',
      filterParams: { values: [true, false] },
      cellRenderer: (p) => <BoolCell value={p.value} />,
    },

    {
      colId:        'status',
      field:        'status',
      headerName:   'Status',
      width:        148,
      filter:       'agNumberColumnFilter',
      sortable:     true,
      cellRenderer: (p) => <StatusCell data={p.data} />,
    },

    {
      colId:        'contacts',
      headerName:   'Contacts',
      width:        170,
      sortable:     false,
      filter:       false,
      resizable:    false,
      cellRenderer: (p) => <ContactsCell data={p.data} />,
    },
  ];
}
