import React, { useId, useState } from 'react'

export function CollapsibleSection({
  title,
  badge = null,
  defaultOpen = true,
  children,
  style,
  className = '',
}) {
  const [open, setOpen] = useState(defaultOpen)
  const contentId = useId()

  return (
    <section
      className={`collapsible-section${open ? ' is-open' : ' is-collapsed'}${className ? ` ${className}` : ''}`}
      style={style}
    >
      <button
        type="button"
        className="section-heading section-toggle"
        aria-expanded={open}
        aria-controls={contentId}
        onClick={() => setOpen(v => !v)}
      >
        <h2>{title}</h2>
        <div className="section-line" />
        {badge}
        <span className="section-chevron" aria-hidden="true">
          <svg width="12" height="12" viewBox="0 0 12 12" fill="none">
            <path d="M3 5l3 3 3-3" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </span>
      </button>

      <div id={contentId} className="section-body" hidden={!open}>
        {children}
      </div>
    </section>
  )
}
