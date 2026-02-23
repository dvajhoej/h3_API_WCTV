import React from 'react'

const STATUS = {
  ok:              { color: 'var(--green)',  label: 'Rent'           },
  let_forvaerring: { color: 'var(--orange)', label: 'Lidt snavset'   },
  forvaerring:     { color: 'var(--red)',    label: 'Skal rengøres'  },
  ugyldig:         { color: 'var(--purple)', label: 'Kontrollér'     },
  inactive:        { color: 'var(--muted)',  label: 'Ikke i brug'    },
}

function formatTime(iso) {
  if (!iso) return '–'
  return new Date(iso).toLocaleTimeString('da-DK', {
    hour: '2-digit', minute: '2-digit', second: '2-digit',
  })
}

function ScoreRing({ score, color }) {
  const R = 22
  const C = 2 * Math.PI * R
  const filled = score !== null ? C * (score / 100) : 0

  return (
    <svg width="58" height="58" viewBox="0 0 58 58" fill="none" xmlns="http://www.w3.org/2000/svg">
      {/* Track */}
      <circle cx="29" cy="29" r={R} stroke="rgba(255,255,255,0.07)" strokeWidth="4.5" fill="none" />
      {/* Arc */}
      {score !== null && (
        <circle
          cx="29" cy="29" r={R}
          stroke={color}
          strokeWidth="4.5"
          fill="none"
          strokeDasharray={`${filled} ${C - filled}`}
          strokeLinecap="round"
          style={{
            transform: 'rotate(-90deg)',
            transformOrigin: '50% 50%',
            transition: 'stroke-dasharray 0.55s ease',
          }}
        />
      )}
      {/* Score label */}
      <text
        x="29" y="33"
        textAnchor="middle"
        fill={color}
        fontSize="11"
        fontWeight="700"
        fontFamily="Inter, system-ui, sans-serif"
      >
        {score !== null ? `${score}%` : '–'}
      </text>
    </svg>
  )
}

export function StallCard({ toilet }) {
  const st        = toilet.status
  const key       = st?.status || 'inactive'
  const cfg       = STATUS[key] || STATUS.inactive
  const score     = st ? Math.round(st.currentScore * 100) : null
  const isOccupied = !!toilet.activeSession

  return (
    <div
      className={`stall-card${isOccupied ? ' stall-occupied' : ''}`}
      style={{ '--stall-color': cfg.color }}
    >
      {/* Row 1: location + occupied badge */}
      <div className="stall-top-row">
        <div className="stall-location">{toilet.location}</div>
        {isOccupied && (
          <div className="occupied-badge">
            <div className="occupied-dot" />
            I brug
          </div>
        )}
      </div>

      {/* Row 2: stall info left, score ring right */}
      <div className="stall-main-row">
        <div>
          <div className="stall-number">Bås {toilet.stallNumber}</div>
          <div className="stall-status-label" style={{ color: cfg.color }}>
            {cfg.label}
          </div>
        </div>
        <ScoreRing score={score} color={cfg.color} />
      </div>

      {/* Row 3: last updated */}
      <div className="stall-time">Sidst kontrolleret: {formatTime(st?.lastUpdated)}</div>
    </div>
  )
}
