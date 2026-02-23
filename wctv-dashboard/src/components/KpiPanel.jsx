import React from 'react'

const KPI_CONFIG = {
  deteriorationRate: {
    label: 'Rengøringsbehov',
    unit: '%',
    color: 'var(--red)',
    bg: 'var(--red-bg)',
    icon: (
      <svg width="15" height="15" viewBox="0 0 15 15" fill="none">
        <path d="M2 4.5l3.5 4 2-2.5 5.5 5.5" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" />
        <path d="M10 12h2.5v-2.5" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    ),
  },
  avgResponseMinutes: {
    label: 'Gns. rengøringstid',
    unit: ' min',
    color: 'var(--orange)',
    bg: 'var(--orange-bg)',
    icon: (
      <svg width="15" height="15" viewBox="0 0 15 15" fill="none">
        <circle cx="7.5" cy="7.5" r="6" stroke="currentColor" strokeWidth="1.6" />
        <path d="M7.5 4.5v3.2l2.2 2.2" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    ),
  },
  okRate: {
    label: 'Rene besøg',
    unit: '%',
    color: 'var(--green)',
    bg: 'var(--green-bg)',
    icon: (
      <svg width="15" height="15" viewBox="0 0 15 15" fill="none">
        <path d="M2.5 8L5.5 11L12.5 4" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    ),
  },
  activeTriggers: {
    label: 'Afventer rengøring',
    unit: '',
    color: 'var(--accent)',
    bg: 'var(--accent-bg)',
    icon: (
      <svg width="15" height="15" viewBox="0 0 15 15" fill="none">
        <path d="M7.5 1L9.3 5.3l4.5.5-3.3 3.2.8 4.5-4-2.1-4 2.1.8-4.5L1.2 5.8l4.5-.5z"
          stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" />
      </svg>
    ),
  },
  totalSessionsThisWeek: {
    label: 'Besøg denne uge',
    unit: '',
    color: 'var(--purple)',
    bg: 'var(--purple-bg)',
    icon: (
      <svg width="15" height="15" viewBox="0 0 15 15" fill="none">
        <rect x="1.5" y="3.5" width="12" height="10" rx="2" stroke="currentColor" strokeWidth="1.6" />
        <path d="M5 3.5V2M10 3.5V2" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" />
        <path d="M1.5 7h12" stroke="currentColor" strokeWidth="1.2" opacity="0.45" />
      </svg>
    ),
  },
}

export function KpiPanel({ kpi }) {
  if (!kpi) return null

  const entries = [
    ['deteriorationRate',    kpi.deteriorationRate],
    ['avgResponseMinutes',   kpi.avgResponseMinutes],
    ['okRate',               kpi.okRate],
    ['activeTriggers',       kpi.activeTriggers],
    ['totalSessionsThisWeek',kpi.totalSessionsThisWeek],
  ]

  return (
    <section style={{ marginBottom: 40 }}>
      <div className="section-heading">
        <h2>Ugeoversigt</h2>
        <div className="section-line" />
      </div>
      <div className="kpi-grid">
        {entries.map(([key, val]) => {
          const cfg = KPI_CONFIG[key]
          return (
            <div
              key={key}
              className="kpi-box"
              style={{ '--kpi-color': cfg.color, '--kpi-bg': cfg.bg }}
            >
              <div className="kpi-glow" />
              <div className="kpi-icon">{cfg.icon}</div>
              <div className="kpi-label">{cfg.label}</div>
              <div className="kpi-value">
                {val ?? '–'}
                {cfg.unit && val != null && (
                  <span className="kpi-unit">{cfg.unit}</span>
                )}
              </div>
            </div>
          )
        })}
      </div>
    </section>
  )
}
