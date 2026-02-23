import { useEffect, useRef } from 'react'
import { createHubConnection } from '../api/client'

export function useSignalR({
  onStatusUpdate,
  onSessionStarted,
  onSessionEnded,
  onTriggerCreated,
  onTriggerUpdated,
}) {
  const connRef = useRef(null)

  useEffect(() => {
    const conn = createHubConnection()
    connRef.current = conn
    let aborted = false

    if (onStatusUpdate)   conn.on('ReceiveStatusUpdate',   onStatusUpdate)
    if (onSessionStarted) conn.on('ReceiveSessionStarted', onSessionStarted)
    if (onSessionEnded)   conn.on('ReceiveSessionEnded',   onSessionEnded)
    if (onTriggerCreated) conn.on('ReceiveTriggerCreated', onTriggerCreated)
    if (onTriggerUpdated) conn.on('ReceiveTriggerUpdated', onTriggerUpdated)

    conn.start().catch(err => {
      // Suppress the expected abort that React StrictMode triggers when it
      // unmounts and remounts effects in development. The second mount will
      // start a fresh connection successfully.
      if (!aborted) {
        console.error('SignalR connection error:', err)
      }
    })

    return () => {
      aborted = true
      conn.stop()
    }
  }, [])
}
