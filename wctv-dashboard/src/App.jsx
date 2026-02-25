import React, { useState, useEffect, useCallback, useMemo } from 'react'
import { StallGrid } from './components/StallGrid'
import { TriggerList } from './components/TriggerList'
import { KpiPanel } from './components/KpiPanel'
import { DailyConditionGraph } from './components/DailyConditionGraph'
import { useSignalR } from './hooks/useSignalR'
import {
  fetchToilets, fetchTriggers, fetchKpi, fetchDailyData,
  acknowledgeTrigger, completeTrigger, falsePositiveTrigger
} from './api/client'

function WctvLogo() {
  return (
    <svg width="46" height="46" viewBox="0 0 46 46" fill="none" xmlns="http://www.w3.org/2000/svg">
      <rect width="46" height="46" rx="11" fill="#172040" />
      <rect x="0.75" y="0.75" width="44.5" height="44.5" rx="10.25" stroke="#3b82f6" strokeWidth="1.5" />
      <rect x="7" y="16" width="22" height="15" rx="3" fill="#3b82f6" />
      <circle cx="18" cy="23.5" r="5.5" fill="#172040" stroke="#60a5fa" strokeWidth="1.5" />
      <circle cx="18" cy="23.5" r="3" fill="#3b82f6" />
      <circle cx="16.8" cy="22.2" r="1" fill="white" opacity="0.65" />
      <path d="M29 18.5L39 14V33L29 28.5Z" fill="#2563eb" />
      <circle cx="27" cy="17" r="2" fill="#f87171" />
      <circle cx="27" cy="17" r="0.9" fill="#fca5a5" />
    </svg>
  )
}

function Clock() {
  const [now, setNow] = useState(new Date())
  useEffect(() => {
    const iv = setInterval(() => setNow(new Date()), 1000)
    return () => clearInterval(iv)
  }, [])
  return (
    <div className="wctv-clock-block">
      <div className="wctv-clock-time">
        {now.toLocaleTimeString('da-DK', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
      </div>
      <div className="wctv-clock-date">
        {now.toLocaleDateString('da-DK', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}
      </div>
    </div>
  )
}

function LiveDot() {
  return (
    <div className="wctv-live">
      <div className="wctv-live-dot-wrap">
        <div className="wctv-live-ring" />
        <div className="wctv-live-core" />
      </div>
      <span className="wctv-live-label">LIVE</span>
    </div>
  )
}

export default function App() {
  const [toilets,    setToilets]    = useState([])
  const [triggers,   setTriggers]   = useState([])
  const [kpi,        setKpi]        = useState(null)
  const [dailyData,  setDailyData]  = useState([])
  const [loading,    setLoading]    = useState(true)

  const loadAll = useCallback(async () => {
    try {
      const [t, tr, k, d] = await Promise.all([
        fetchToilets(), fetchTriggers(), fetchKpi(), fetchDailyData()
      ])
      setToilets(t)
      setTriggers(tr)
      setKpi(k)
      setDailyData(d)
    } catch (e) {
      console.error('Load error:', e)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { loadAll() }, [loadAll])

  // Merge triggers into toilets so Toiletbåse and Rengøringsopgaver reflect the same data.
  const triggersByToilet = useMemo(() => {
    const byToilet = new Map()
    const isBetter = (a, b) => {
      const aAck = a.status === 'acknowledged' ? 1 : 0
      const bAck = b.status === 'acknowledged' ? 1 : 0
      if (aAck !== bAck) return aAck > bAck
      const aSev = a.severity === 'forvaerring' ? 1 : 0
      const bSev = b.severity === 'forvaerring' ? 1 : 0
      if (aSev !== bSev) return aSev > bSev
      return new Date(a.createdAt).getTime() > new Date(b.createdAt).getTime()
    }

    for (const t of triggers) {
      const existing = byToilet.get(t.toiletId)
      if (!existing || isBetter(t, existing)) {
        byToilet.set(t.toiletId, t)
      }
    }
    return byToilet
  }, [triggers])

  const toiletsForDisplay = useMemo(() => (
    toilets.map(toilet => {
      const trig = triggersByToilet.get(toilet.id)
      if (!trig) return toilet

      const statusKey = trig.severity === 'forvaerring' ? 'forvaerring' : 'let_forvaerring'
      const baseStatus = toilet.status ?? { currentScore: null, lastUpdated: trig.createdAt }

      return {
        ...toilet,
        status: { ...baseStatus, status: statusKey },
      }
    })
  ), [toilets, triggersByToilet])

  // Refresh KPI + graph every 30 s
  useEffect(() => {
    const iv = setInterval(() => {
      fetchKpi().then(setKpi).catch(console.error)
      fetchDailyData().then(setDailyData).catch(console.error)
    }, 30000)
    return () => clearInterval(iv)
  }, [])

  useSignalR({
    onStatusUpdate: useCallback(({ toiletId, status, score }) => {
      setToilets(prev => prev.map(t =>
        t.id === toiletId
          ? { ...t, status: { ...t.status, status, currentScore: score, lastUpdated: new Date().toISOString() } }
          : t
      ))
    }, []),

    onSessionStarted: useCallback(({ toiletId, sessionId }) => {
      setToilets(prev => prev.map(t =>
        t.id === toiletId
          ? { ...t, activeSession: { id: sessionId, startedAt: new Date().toISOString() } }
          : t
      ))
    }, []),

    onSessionEnded: useCallback(({ toiletId }) => {
      setToilets(prev => prev.map(t =>
        t.id === toiletId ? { ...t, activeSession: null } : t
      ))
      fetchDailyData().then(setDailyData).catch(console.error)
    }, []),

    onTriggerCreated: useCallback(({ trigger }) => {
      setTriggers(prev => [trigger, ...prev.filter(t => t.toiletId !== trigger.toiletId)])
      fetchKpi().then(setKpi).catch(console.error)
    }, []),

    onTriggerUpdated: useCallback(({ triggerId, status }) => {
      if (status === 'completed' || status === 'false_positive') {
        setTriggers(prev => prev.filter(t => t.id !== triggerId))
      } else {
        setTriggers(prev => prev.map(t =>
          t.id === triggerId ? { ...t, status } : t
        ))
      }
      fetchKpi().then(setKpi).catch(console.error)
    }, []),
  })

  const handleAcknowledge = async (id) => {
    await acknowledgeTrigger(id)
    setTriggers(prev => prev.map(t => t.id === id ? { ...t, status: 'acknowledged' } : t))
  }

  const handleComplete = async (id) => {
    await completeTrigger(id)
    setTriggers(prev => prev.filter(t => t.id !== id))
    fetchKpi().then(setKpi).catch(console.error)
    fetchDailyData().then(setDailyData).catch(console.error)
  }

  const handleFalsePositive = async (id) => {
    await falsePositiveTrigger(id)
    setTriggers(prev => prev.filter(t => t.id !== id))
  }

  return (
    <div className="wctv-app">
      <header className="wctv-header">
        <div className="wctv-header-brand">
          <WctvLogo />
          <div>
            <div className="wctv-brand-name">WCTV</div>
            <div className="wctv-brand-sub">
              Toiletmonitor&nbsp;·&nbsp;Techcollege
            </div>
          </div>
        </div>

        <div className="wctv-header-right">
          <Clock />
          <div className="wctv-header-divider" />
          <LiveDot />
        </div>
      </header>

      {loading ? (
        <div className="wctv-loading">
          <svg className="wctv-spinner" width="22" height="22" viewBox="0 0 22 22" fill="none">
            <circle cx="11" cy="11" r="9" stroke="rgba(255,255,255,0.08)" strokeWidth="3" />
            <path d="M11 2a9 9 0 0 1 9 9" stroke="#3b82f6" strokeWidth="3" strokeLinecap="round" />
          </svg>
          Indlæser…
        </div>
      ) : (
        <>
          <KpiPanel kpi={kpi} />
          <DailyConditionGraph data={dailyData} />
          <StallGrid toilets={toiletsForDisplay} />
          <TriggerList
            triggers={triggers}
            onAcknowledge={handleAcknowledge}
            onComplete={handleComplete}
            onFalsePositive={handleFalsePositive}
          />
        </>
      )}
    </div>
  )
}
