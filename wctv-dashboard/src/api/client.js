import * as signalR from '@microsoft/signalr'

const BASE = '/api'

export async function fetchToilets() {
  const res = await fetch(`${BASE}/toilets`)
  if (!res.ok) throw new Error('Failed to fetch toilets')
  return res.json()
}

export async function fetchTriggers() {
  const res = await fetch(`${BASE}/triggers`)
  if (!res.ok) throw new Error('Failed to fetch triggers')
  return res.json()
}

export async function fetchKpi() {
  const res = await fetch(`${BASE}/kpi`)
  if (!res.ok) throw new Error('Failed to fetch KPI')
  return res.json()
}

export async function acknowledgeTrigger(id) {
  const res = await fetch(`${BASE}/triggers/${id}/acknowledge`, { method: 'PATCH' })
  if (!res.ok) throw new Error('Failed to acknowledge')
  return res.json()
}

export async function completeTrigger(id) {
  const res = await fetch(`${BASE}/triggers/${id}/complete`, { method: 'PATCH' })
  if (!res.ok) throw new Error('Failed to complete')
  return res.json()
}

export async function falsePositiveTrigger(id) {
  const res = await fetch(`${BASE}/triggers/${id}/false-positive`, { method: 'PATCH' })
  if (!res.ok) throw new Error('Failed to mark false positive')
  return res.json()
}

export async function fetchDailyData() {
  const res = await fetch('/api/kpi/daily')
  if (!res.ok) throw new Error('Failed to fetch daily data')
  return res.json()
}

export function createHubConnection() {
  return new signalR.HubConnectionBuilder()
    .withUrl('/hub/dashboard')
    .withAutomaticReconnect()
    .build()
}
