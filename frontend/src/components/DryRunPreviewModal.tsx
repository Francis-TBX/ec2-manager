import { useQuery } from '@tanstack/react-query'
import { startInstances, stopInstances } from '../api/instances'

interface Props {
  action: 'start' | 'stop'
  accountKey: string
  region: string
  instanceIds: string[]
  onConfirm: () => void
  onCancel: () => void
  isConfirming: boolean
}

export default function DryRunPreviewModal({ action, accountKey, region, instanceIds, onConfirm, onCancel, isConfirming }: Props) {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['dryRun', action, accountKey, region, instanceIds],
    queryFn: () =>
      action === 'start'
        ? startInstances(accountKey, region, instanceIds, true)
        : stopInstances(accountKey, region, instanceIds, true),
  })

  const verb = action === 'start' ? 'Start' : 'Stop'

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl shadow-xl max-w-md w-full p-5 space-y-4">
        <h2 className="text-lg font-bold text-slate-800">Confirm {verb}</h2>

        {isLoading ? (
          <div className="text-sm text-slate-400 py-4 text-center">Checking what would happen…</div>
        ) : isError ? (
          <div className="text-sm text-red-500 py-4 text-center">Failed to run dry-run preview.</div>
        ) : (
          <div className="space-y-3 text-sm">
            <div className="flex items-center gap-2">
              <span className="w-2 h-2 rounded-full bg-green-500" />
              <span>Would {action}: <strong>{data!.affected.length}</strong></span>
            </div>

            {data!.skipped.length > 0 && (
              <div>
                <div className="flex items-center gap-2 mb-1">
                  <span className="w-2 h-2 rounded-full bg-amber-500" />
                  <span>Would skip: <strong>{data!.skipped.length}</strong></span>
                </div>
                <ul className="ml-4 space-y-0.5 text-slate-500 text-xs">
                  {data!.skipped.map((s) => (
                    <li key={s.instanceId}>{s.instanceId} — {s.reason}</li>
                  ))}
                </ul>
              </div>
            )}

            {data!.errors.length > 0 && (
              <div className="text-red-600 text-xs">
                {data!.errors.join('; ')}
              </div>
            )}
          </div>
        )}

        <div className="flex justify-end gap-2 pt-2">
          <button
            onClick={onCancel}
            className="text-sm bg-white border border-slate-300 rounded-lg px-3 py-1.5"
          >
            Cancel
          </button>
          <button
            onClick={onConfirm}
            disabled={isLoading || isError || isConfirming || (data?.affected.length ?? 0) === 0}
            className={`text-sm text-white rounded-lg px-3 py-1.5 disabled:opacity-50 ${
              action === 'start' ? 'bg-green-600 hover:bg-green-700' : 'bg-red-600 hover:bg-red-700'
            }`}
          >
            {isConfirming ? 'Working…' : `Confirm ${verb}`}
          </button>
        </div>
      </div>
    </div>
  )
}
