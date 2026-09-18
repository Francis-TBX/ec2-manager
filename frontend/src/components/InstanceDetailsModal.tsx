import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getInstanceDetail, startInstances, stopInstances } from '../api/instances'
import { listLogs } from '../api/logs'
import StatusBadge from './StatusBadge'
import DryRunPreviewModal from './DryRunPreviewModal'

interface Props {
  instanceId: string
  accountKey: string
  region: string
  onClose: () => void
}

export default function InstanceDetailsModal({ instanceId, accountKey, region, onClose }: Props) {
  const queryClient = useQueryClient()
  const [pendingAction, setPendingAction] = useState<'start' | 'stop' | null>(null)

  const { data: detail, isLoading, isError } = useQuery({
    queryKey: ['instanceDetail', instanceId, accountKey, region],
    queryFn: () => getInstanceDetail(instanceId, accountKey, region),
  })

  const { data: logs } = useQuery({
    queryKey: ['instanceLogs', instanceId],
    queryFn: () => listLogs({ instanceId, pageSize: 10 }),
  })

  const actionMutation = useMutation({
    mutationFn: async (action: 'start' | 'stop') =>
      action === 'start'
        ? startInstances(accountKey, region, [instanceId], false)
        : stopInstances(accountKey, region, [instanceId], false),
    onSuccess: () => {
      setPendingAction(null)
      queryClient.invalidateQueries({ queryKey: ['instances'] })
      queryClient.invalidateQueries({ queryKey: ['instanceDetail', instanceId] })
      queryClient.invalidateQueries({ queryKey: ['instanceLogs', instanceId] })
    },
  })

  return (
    <>
      <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-40 p-4" onClick={onClose}>
        <div
          className="bg-white rounded-xl shadow-xl max-w-lg w-full max-h-[85vh] overflow-y-auto p-5 space-y-4"
          onClick={(e) => e.stopPropagation()}
        >
          <div className="flex justify-between items-start">
            <h2 className="text-lg font-bold text-slate-800">Instance Details</h2>
            <button onClick={onClose} className="text-slate-400 hover:text-slate-600 text-xl leading-none">×</button>
          </div>

          {isLoading ? (
            <div className="text-sm text-slate-400 py-8 text-center">Loading…</div>
          ) : isError || !detail ? (
            <div className="text-sm text-red-500 py-8 text-center">Failed to load instance.</div>
          ) : (
            <>
              <div className="space-y-1">
                <div className="font-medium text-slate-800">{detail.name}</div>
                <div className="text-xs text-slate-400">{detail.instanceId}</div>
                <div className="flex items-center gap-2 pt-1">
                  <StatusBadge state={detail.state} />
                  <span className={`text-xs px-2 py-0.5 rounded-full ${detail.dnsEnabled ? 'bg-teal-100 text-teal-800' : 'bg-slate-100 text-slate-500'}`}>
                    Protected: {detail.dnsEnabled ? 'True' : 'False'}
                  </span>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-2 text-sm">
                <div><span className="text-slate-400">Type:</span> {detail.instanceType ?? '—'}</div>
                <div><span className="text-slate-400">Region:</span> {detail.region}</div>
                <div><span className="text-slate-400">Public IP:</span> {detail.publicIp ?? '—'}</div>
                <div><span className="text-slate-400">Private IP:</span> {detail.privateIp ?? '—'}</div>
                <div className="col-span-2"><span className="text-slate-400">Launched:</span> {detail.launchTime ?? '—'}</div>
              </div>

              <div>
                <div className="text-xs font-medium text-slate-500 mb-1">Tags</div>
                <div className="flex flex-wrap gap-1.5">
                  {detail.tags.length === 0 ? (
                    <span className="text-xs text-slate-400">No tags</span>
                  ) : (
                    detail.tags.map((t) => (
                      <span key={t.key} className="text-xs bg-slate-100 text-slate-600 rounded px-2 py-0.5">
                        {t.key}={t.value}
                      </span>
                    ))
                  )}
                </div>
              </div>

              <div className="flex gap-2">
                <button
                  onClick={() => setPendingAction('start')}
                  className="text-sm bg-green-600 hover:bg-green-700 text-white rounded-lg px-3 py-1.5"
                >
                  Start
                </button>
                <button
                  onClick={() => setPendingAction('stop')}
                  className="text-sm bg-red-600 hover:bg-red-700 text-white rounded-lg px-3 py-1.5"
                >
                  Stop
                </button>
              </div>

              <div>
                <div className="text-xs font-medium text-slate-500 mb-1">Recent activity</div>
                {!logs || logs.items.length === 0 ? (
                  <div className="text-xs text-slate-400">No recent actions.</div>
                ) : (
                  <ul className="space-y-1.5">
                    {logs.items.map((l) => (
                      <li key={l.id} className="text-xs border-b border-slate-100 pb-1.5">
                        <span className="font-medium text-slate-700">{l.actionType}</span>{' '}
                        <span className="text-slate-400">{new Date(l.timestamp).toLocaleString()}</span>{' '}
                        <span className={l.result === 'Success' ? 'text-teal-600' : l.result === 'Failed' ? 'text-red-600' : 'text-amber-600'}>
                          {l.result}
                        </span>
                        <div className="text-slate-500">{l.message}</div>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </>
          )}
        </div>
      </div>

      {pendingAction && (
        <DryRunPreviewModal
          action={pendingAction}
          accountKey={accountKey}
          region={region}
          instanceIds={[instanceId]}
          isConfirming={actionMutation.isPending}
          onCancel={() => setPendingAction(null)}
          onConfirm={() => actionMutation.mutate(pendingAction)}
        />
      )}
    </>
  )
}
