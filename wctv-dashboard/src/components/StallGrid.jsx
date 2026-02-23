import React from 'react'
import { StallCard } from './StallCard'

export function StallGrid({ toilets }) {
  const okCount = toilets.filter(t => t.status?.status === 'ok').length
  const total   = toilets.length

  return (
    <section style={{ marginBottom: 40 }}>
      <div className="section-heading">
        <h2>Toiletbåse</h2>
        <div className="section-line" />
        {total > 0 && (
          <div
            className="section-badge"
            style={okCount === total
              ? { color: 'var(--green)', borderColor: 'rgba(34,197,94,0.3)' }
              : {}
            }
          >
            {okCount}/{total} rene
          </div>
        )}
      </div>
      <div className="stall-grid">
        {toilets.map(t => <StallCard key={t.id} toilet={t} />)}
      </div>
    </section>
  )
}
