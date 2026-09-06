import React, { useState } from 'react';
import {
  RefreshCw, SlidersHorizontal, X, UserCheck,
} from 'lucide-react';

/**
 * @param {{
 *   onRefresh: () => void;
 *   isLoading: boolean;
 *   activeFilterCount: number;
 *   onClearFilters: () => void;
 *   selectedCount: number;
 *   selectedRows?: any[];
 *   canEditStatus?: boolean;
 *   onBulkStatusChange?: (selectedRows: any[], newStatus: string) => void;
 *   density: 'compact'|'normal'|'comfortable';
 *   onDensityChange: (d: 'compact'|'normal'|'comfortable') => void;
 * }} props
 */
export default function StudentGridToolbar({
  onRefresh, isLoading,
  activeFilterCount, onClearFilters,
  selectedCount,
  selectedRows = [],
  canEditStatus = false,
  onBulkStatusChange,
  density, onDensityChange,
}) {
  const [bulkStatus, setBulkStatus] = useState('Accepted');
  
  return (
    <div className="sg-toolbar" role="toolbar" aria-label="Grid controls">
      {/* ── Left ── */}
      <div className="sg-toolbar-left" style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
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

        {/* Bulk status change widget */}
        {selectedCount > 0 && canEditStatus && (
          <div style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 6,
            background: 'rgba(239, 49, 49, 0.08)',
            border: '1px solid rgba(239, 49, 49, 0.25)',
            borderRadius: 8,
            padding: '2px 8px',
            fontSize: 12,
          }}>
            <UserCheck size={14} style={{ color: '#ef3131' }} />
            <span style={{ fontWeight: 600, color: '#374151', whiteSpace: 'nowrap' }}>
              Change Status:
            </span>
            <select
              value={bulkStatus}
              onChange={(e) => setBulkStatus(e.target.value)}
              style={{
                background: '#ffffff',
                border: '1px solid #d1d5db',
                borderRadius: 6,
                padding: '2px 6px',
                fontSize: 12,
                fontWeight: 600,
                color: '#111827',
                cursor: 'pointer',
                outline: 'none',
              }}
            >
              <option value="Accepted">Accepted</option>
              <option value="Pending">Pending</option>
              <option value="Waitlisted">Waitlisted</option>
              <option value="Rejected">Rejected</option>
            </select>
            <button
              onClick={() => onBulkStatusChange && onBulkStatusChange(selectedRows, bulkStatus)}
              style={{
                background: '#ef3131',
                color: '#ffffff',
                border: 'none',
                borderRadius: 6,
                padding: '3px 10px',
                fontSize: 12,
                fontWeight: 700,
                cursor: 'pointer',
                whiteSpace: 'nowrap',
                transition: 'background 0.15s ease',
              }}
              onMouseEnter={(e) => (e.currentTarget.style.background = '#dc2626')}
              onMouseLeave={(e) => (e.currentTarget.style.background = '#ef3131')}
            >
              Apply ({selectedCount})
            </button>
          </div>
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
