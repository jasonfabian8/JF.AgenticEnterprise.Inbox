import { createContext, useContext, type ReactNode } from 'react'

// Sprint 0 stub — SignalR connection wired in Sprint 1
interface AgentEventContextValue {
  isConnected: boolean
}

const AgentEventContext = createContext<AgentEventContextValue>({ isConnected: false })

export function AgentEventProvider({ children }: { children: ReactNode }) {
  return (
    <AgentEventContext.Provider value={{ isConnected: false }}>
      {children}
    </AgentEventContext.Provider>
  )
}

export const useAgentEvents = () => useContext(AgentEventContext)
