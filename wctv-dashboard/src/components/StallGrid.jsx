import React from 'react'
import { StallCard } from './StallCard'
import { CollapsibleSection } from './CollapsibleSection'

export function StallGrid({ toilets }) {
  const okCount = toilets.filter(t => t.status?.status === 'ok').length
  const total   = toilets.length

  return (
    <CollapsibleSection
      title="Toiletbåse"
      style={{ marginBottom: 40 }}
      badge={total > 0 ? (
        <div
          className="section-badge"
          style={okCount === total
            ? { color: 'var(--green)', borderColor: 'rgba(34,197,94,0.3)' }
            : {}
          }
        >
          {okCount}/{total} rene
        </div>
      ) : null}
    >
      <div className="stall-grid">
        {toilets.map(t => <StallCard key={t.id} toilet={t} />)}
      </div>
    </CollapsibleSection>
  )
}
