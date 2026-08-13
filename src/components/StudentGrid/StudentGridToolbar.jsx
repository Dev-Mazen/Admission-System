import React, { useState, useRef, useEffect } from 'react';
import {
  Search, RefreshCw, Columns, Download, SlidersHorizontal,
  Sparkles, X, ChevronDown,
} from 'lucide-react';
import { clearPrefs } from './studentGrid.prefs.js';
import * as XLSX from 'xlsx';

/**
 * Column metadata for the manager panel.
 * @typedef {{ colId: string; label: string; hidden: boolean }} ManagedCol
 */

/**
 * @param {{
 *   search: string;
 *   onSearch: (v: string) => void;
 *   onRefresh: () => void;
 *   isLoading: boolean;
 *   activeFilterCount: number;
 *   onClearFilters: () => void;
 *   selectedCount: number;
 *   density: 'compact'|'normal'|'comfortable';
 *   onDensityChange: (d: 'compact'|'normal'|'comfortable') => void;
 *   showAI: boolean;
 *   onToggleAI: () => void;
 *   managedCols: ManagedCol[];
 *   onToggleCol: (colId: string) => void;
 *   onResetCols: () => void;
 *   gridApi: import('ag-grid-community').GridApi | null;
 *   rowData: object[];
 * }} props
 */
export default function StudentGridToolbar({
  search, onSearch, onRefresh, isLoading,
  activeFilterCount, onClearFilters,
  selectedCount,
  density, onDensityChange,
  showAI, onToggleAI,
  managedCols, onToggleCol, onResetCols,
  gridApi, rowData,
}) {
  const [colOpen, setColOpen] = useState(false);
  const colRef  = useRef(null);

  // Close col manager popover on outside click
  useEffect(() => {
    const handler = (e) => {
      if (colRef.current && !colRef.current.contains(e.target)) setColOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  /** Export visible rows as CSV via AG Grid */
  const exportCSV = () => {
    if (!gridApi) return;
    gridApi.exportDataAsCsv({
      fileName: `students_${new Date().toISOString().slice(0, 10)}.csv`,
      skipColumnGroupHeaders: true,
    });
  };

  /** Export visible rows as Excel via xlsx */
  const exportExcel = () => {
    if (!rowData?.length) return;
    const ws = XLSX.utils.json_to_sheet(rowData);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Students');
    XLSX.writeFile(wb, `students_${new Date().toISOString().slice(0, 10)}.xlsx`);
  };

  const hiddenCount = managedCols.filter(c => c.hidden).length;

  return (
    <div className="sg-toolbar" role="toolbar" aria-label="Grid controls">
      {/* ── Left ── */}
      <div className="sg-toolbar-left">
        {/* Search */}
        <div className="sg-search-wrap">
          <Search className="sg-search-icon" aria-hidden />
          <input
            className="sg-search-input"
            type="search"
            value={search}
            onChange={e => onSearch(e.target.value)}
            placeholder="Search students…"
            aria-label="Search students"
          />
        </div>

        {/* Active filter badge */}
        {activeFilterCount > 0 && (
          <button
            className="sg-btn active"
            onClick={onClearFilters}
            title="Clear all column filters"
            aria-label={`${activeFilterCount} active filters — click to clear`}
          >
            <SlidersHorizontal size={13} />
            {activeFilterCount} filter{activeFilterCount !== 1 ? 's' : ''}
            <X size={11} />
          </button>
        )}

        {/* Selection badge */}
        {selectedCount > 0 && (
          <span className="sg-selection-badge" role="status" aria-live="polite">
            {selectedCount} selected
          </span>
        )}
      </div>

      {/* ── Right ── */}
      <div className="sg-toolbar-right">
        {/* Density */}
        <div className="sg-density-group" role="group" aria-label="Row density">
          {(['compact', 'normal', 'comfortable']).map(d => (
            <button
              key={d}
              className={`sg-density-opt${density === d ? ' active' : ''}`}
              onClick={() => onDensityChange(d)}
              aria-pressed={density === d}
              title={`${d.charAt(0).toUpperCase() + d.slice(1)} density`}
            >
              {d === 'compact' ? '▤' : d === 'normal' ? '▥' : '▦'}
            </button>
          ))}
        </div>

        {/* Column manager */}
        <div className="sg-popover-wrap" ref={colRef}>
          <button
            className={`sg-btn${hiddenCount > 0 ? ' active' : ''}`}
            onClick={() => setColOpen(v => !v)}
            aria-expanded={colOpen}
            aria-haspopup="dialog"
            title="Manage columns"
          >
            <Columns size={14} />
            Columns
            {hiddenCount > 0 && <span className="sg-badge">{hiddenCount}</span>}
            <ChevronDown size={11} style={{ opacity: 0.5 }} />
          </button>

          {colOpen && (
            <div className="sg-popover" role="dialog" aria-label="Column visibility">
              <div className="sg-popover-title">
                <span>Columns</span>
                <button
                  className="sg-btn"
                  style={{ padding: '2px 6px', fontSize: 11 }}
                  onClick={() => { onResetCols(); clearPrefs(); setColOpen(false); }}
                  title="Reset to default columns"
                >
                  Reset
                </button>
              </div>
              {managedCols.map(col => (
                <div key={col.colId} className="sg-col-item">
                  <input
                    type="checkbox"
                    id={`col-${col.colId}`}
                    checked={!col.hidden}
                    onChange={() => onToggleCol(col.colId)}
                    aria-label={`Toggle ${col.label} column`}
                  />
                  <label htmlFor={`col-${col.colId}`}>{col.label}</label>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Export */}
        <div className="sg-popover-wrap" style={{ position: 'relative' }}>
          <ExportDropdown onCSV={exportCSV} onExcel={exportExcel} />
        </div>

        {/* Refresh */}
        <button
          className="sg-btn"
          onClick={onRefresh}
          disabled={isLoading}
          title="Refresh data"
          aria-label="Refresh student list"
        >
          <RefreshCw
            size={14}
            style={{
              animation: isLoading ? 'sg-spin 0.7s linear infinite' : 'none',
            }}
          />
        </button>
      </div>
    </div>
  );
}

/** Small export split dropdown */
function ExportDropdown({ onCSV, onExcel }) {
  const [open, setOpen] = useState(false);
  const ref = useRef(null);

  useEffect(() => {
    const h = (e) => { if (ref.current && !ref.current.contains(e.target)) setOpen(false); };
    document.addEventListener('mousedown', h);
    return () => document.removeEventListener('mousedown', h);
  }, []);

  return (
    <div ref={ref}>
      <button
        className="sg-btn"
        onClick={() => setOpen(v => !v)}
        aria-expanded={open}
        title="Export data"
      >
        <Download size={14} />
        Export
        <ChevronDown size={11} style={{ opacity: 0.5 }} />
      </button>
      {open && (
        <div
          className="sg-popover"
          style={{ minWidth: 140, padding: 6 }}
          role="menu"
        >
          <button
            className="sg-col-item"
            role="menuitem"
            onClick={() => { onCSV(); setOpen(false); }}
            style={{ width: '100%' }}
          >
            <label style={{ cursor: 'pointer' }}>CSV (.csv)</label>
          </button>
          <button
            className="sg-col-item"
            role="menuitem"
            onClick={() => { onExcel(); setOpen(false); }}
            style={{ width: '100%' }}
          >
            <label style={{ cursor: 'pointer' }}>Excel (.xlsx)</label>
          </button>
        </div>
      )}
    </div>
  );
}
