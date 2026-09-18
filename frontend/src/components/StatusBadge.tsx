const STATE_STYLES: Record<string, string> = {
  running: 'bg-green-100 text-green-800',
  stopped: 'bg-slate-200 text-slate-700',
  pending: 'bg-amber-100 text-amber-800',
  stopping: 'bg-amber-100 text-amber-800',
  'shutting-down': 'bg-red-100 text-red-800',
  terminated: 'bg-red-100 text-red-800',
}

export default function StatusBadge({ state }: { state: string }) {
  const style = STATE_STYLES[state] ?? 'bg-slate-100 text-slate-600'
  return (
    <span className={`inline-block px-2.5 py-0.5 rounded-full text-xs font-medium ${style}`}>
      {state}
    </span>
  )
}
