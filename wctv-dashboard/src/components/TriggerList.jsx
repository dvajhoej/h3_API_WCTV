import React from 'react'

const SEVERITY = {
  let:        { label: 'Lidt snavset',  color: 'var(--orange)', bg: 'var(--orange-bg)', border: 'rgba(245,158,11,0.35)'  },
  forvaerring:{ label: 'Skal rengøres', color: 'var(--red)',    bg: 'var(--red-bg)',    border: 'rgba(239,68,68,0.35)'   },
}

function formatDateTime(iso) {
  if (!iso) return '–'
  return new Date(iso).toLocaleString('da-DK', {
    day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit',
  })
}

function ConfBar({ value }) {
  const pct   = Math.round(value * 100)
  const color = pct >= 80 ? 'var(--red)' : pct >= 60 ? 'var(--orange)' : 'var(--muted)'
  return (
    <div className="trigger-conf-wrap">
      <div className="trigger-conf-label">Systemsikkerhed</div>
      <div className="trigger-conf-bar-track">
        <div
          className="trigger-conf-bar-fill"
          style={{ width: `${pct}%`, background: color }}
        />
      </div>
      <div className="trigger-conf-value">{pct}%</div>
    </div>
  )
}

export function TriggerList({ triggers, onAcknowledge, onComplete, onFalsePositive }) {
  return (
    <section style={{ marginBottom: 40 }}>
      <div className="section-heading">
        <h2>Rengøringsopgaver</h2>
        <div className="section-line" />
        {triggers.length > 0 && (
          <div
            className="section-badge"
            style={{ color: 'var(--red)', borderColor: 'rgba(239,68,68,0.3)' }}
          >
            {triggers.length} afventer
          </div>
        )}
      </div>

      {triggers.length === 0 ? (
        <div className="empty-state">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <path
              d="M8 1.5a6.5 6.5 0 1 0 0 13 6.5 6.5 0 0 0 0-13zM8 5v4.5M8 11.5v.5"
              stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"
            />
          </svg>
          Ingen afventende rengøringer — alle toiletter er i orden
        </div>
      ) : (
        <div className="trigger-list">
          {triggers.map(t => {
            const sev = SEVERITY[t.severity] || { label: t.severity, color: 'var(--muted)', bg: 'var(--muted-bg)', border: 'rgba(100,116,139,0.3)' }
            const isAck = t.status === 'acknowledged'

            return (
              <div key={t.id} className="trigger-row" style={{ '--trig-color': sev.color }}>
                {/* Left: severity pill + toilet name */}
                <div>
                  <div
                    className="trigger-sev-pill"
                    style={{
                      color: sev.color,
                      background: sev.bg,
                      border: `1px solid ${sev.border}`,
                    }}
                  >
                    <svg width="7" height="7" viewBox="0 0 7 7" fill="currentColor">
                      <circle cx="3.5" cy="3.5" r="3.5" />
                    </svg>
                    {sev.label}
                  </div>
                  <div className="trigger-name">{t.toiletName}</div>
                </div>

                {/* Confidence bar */}
                <ConfBar value={t.confidence} />

                {/* Time + status */}
                <div className="trigger-meta">
                  <div className="trigger-time">{formatDateTime(t.createdAt)}</div>
                  <div
                    className="trigger-status-pill"
                    style={isAck
                      ? { background: 'rgba(59,130,246,0.1)',  color: 'var(--accent)', border: '1px solid rgba(59,130,246,0.3)'  }
                      : { background: 'rgba(239,68,68,0.1)',   color: 'var(--red)',    border: '1px solid rgba(239,68,68,0.3)'   }
                    }
                  >
                    {isAck ? 'Er på vej' : 'Ny opgave'}
                  </div>
                </div>

                {/* Actions */}
                <div className="trigger-actions">
                  {t.status === 'active' && (
                    <button className="btn btn-primary" onClick={() => onAcknowledge(t.id)}>
                      Er på vej
                    </button>
                  )}
                  {(t.status === 'active' || t.status === 'acknowledged') && (
                    <button className="btn btn-success" onClick={() => onComplete(t.id)}>
                      Rengøring færdig
                    </button>
                  )}
                  <button
                    className="btn btn-ghost"
                    onClick={() => { if (window.confirm('Markér som intet behov?')) onFalsePositive(t.id) }}
                  >
                    Intet behov
                  </button>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </section>
  )
}
