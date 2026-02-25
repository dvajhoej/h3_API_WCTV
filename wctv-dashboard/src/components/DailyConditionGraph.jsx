import React from 'react'
import { CollapsibleSection } from './CollapsibleSection'
import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid,
  Tooltip, ResponsiveContainer, ReferenceLine,
} from 'recharts'

function CustomTooltip({ active, payload }) {
  if (!active || !payload?.length) return null
  const d = payload[0].payload
  return (
    <div style={{
      background: '#1e293b',
      border: '1px solid rgba(255,255,255,0.11)',
      borderRadius: 8,
      padding: '10px 14px',
      fontSize: 12,
      color: '#f1f5f9',
      lineHeight: 1.7,
    }}>
      <div style={{ fontWeight: 700, marginBottom: 3 }}>
        Kl. {String(d.hour).padStart(2, '0')}:00
      </div>
      <div>
        Renhed:&nbsp;
        <strong style={{ color: d.avgScore >= 75 ? '#22c55e' : d.avgScore >= 55 ? '#f59e0b' : '#ef4444' }}>
          {d.avgScore}%
        </strong>
      </div>
      <div style={{ color: '#94a3b8' }}>{d.visits} besøg</div>
      {d.needsCleaning > 0 && (
        <div style={{ color: '#f59e0b' }}>
          {d.needsCleaning} krævede rengøring
        </div>
      )}
    </div>
  )
}

function scoreColor(avgScore) {
  if (avgScore >= 75) return '#22c55e'
  if (avgScore >= 55) return '#f59e0b'
  return '#ef4444'
}

export function DailyConditionGraph({ data }) {
  // Pick line/fill color from the overall average
  const avg = data?.length
    ? data.reduce((s, d) => s + d.avgScore, 0) / data.length
    : 80
  const color = scoreColor(avg)

  return (
    <CollapsibleSection title="Tilstand i dag" style={{ marginBottom: 40 }}>
      <div style={{
        background: 'var(--surf-1)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--r)',
        padding: '20px 16px 10px',
      }}>
        {!data || data.length === 0 ? (
          <div style={{
            height: 180,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'var(--t3)',
            fontSize: 13,
          }}>
            Grafen udfyldes i løbet af dagen — ingen besøg registreret endnu
          </div>
        ) : (
          <ResponsiveContainer width="100%" height={200}>
            <AreaChart data={data} margin={{ top: 6, right: 16, left: -10, bottom: 0 }}>
              <defs>
                <linearGradient id="condGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%"  stopColor={color} stopOpacity={0.22} />
                  <stop offset="95%" stopColor={color} stopOpacity={0.02} />
                </linearGradient>
              </defs>

              <CartesianGrid
                strokeDasharray="3 3"
                stroke="rgba(255,255,255,0.05)"
                vertical={false}
              />

              <XAxis
                dataKey="hour"
                tickFormatter={h => `${String(h).padStart(2, '0')}:00`}
                tick={{ fill: '#64748b', fontSize: 11 }}
                axisLine={false}
                tickLine={false}
              />

              <YAxis
                domain={[0, 100]}
                tickFormatter={v => `${v}%`}
                tick={{ fill: '#64748b', fontSize: 11 }}
                axisLine={false}
                tickLine={false}
                width={44}
              />

              <Tooltip content={<CustomTooltip />} cursor={{ stroke: 'rgba(255,255,255,0.1)', strokeWidth: 1 }} />

              {/* Threshold line — below this, cleaning is usually triggered */}
              <ReferenceLine
                y={75}
                stroke="#f59e0b"
                strokeDasharray="5 4"
                strokeOpacity={0.55}
                label={{ value: 'Rengøringsgrænse', fill: '#f59e0b', fontSize: 10, position: 'insideTopRight' }}
              />

              <Area
                type="monotone"
                dataKey="avgScore"
                stroke={color}
                strokeWidth={2}
                fill="url(#condGrad)"
                dot={{ fill: color, r: 3, strokeWidth: 0 }}
                activeDot={{ r: 5, fill: color, strokeWidth: 0 }}
              />
            </AreaChart>
          </ResponsiveContainer>
        )}
      </div>
    </CollapsibleSection>
  )
}
