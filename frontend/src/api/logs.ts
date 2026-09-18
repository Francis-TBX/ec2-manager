import client from './client'

export interface AuditLog {
  id: number
  timestamp: string
  userName: string | null
  actionType: string
  accountKey: string
  region: string
  instanceIds: string[]
  dryRun: boolean
  result: string
  message: string
  error: string | null
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export interface ListLogsParams {
  instanceId?: string
  accountKey?: string[]
  region?: string[]
  actionType?: string[]
  result?: string[]
  from?: string
  to?: string
  page?: number
  pageSize?: number
}

export async function listLogs(params: ListLogsParams): Promise<PagedResult<AuditLog>> {
  const { instanceId, accountKey, region, actionType, result, from, to, page, pageSize } = params
  const { data } = await client.get<PagedResult<AuditLog>>('/logs', {
    params: {
      instanceId,
      accountKey: accountKey && accountKey.length > 0 ? accountKey.join(',') : undefined,
      region: region && region.length > 0 ? region.join(',') : undefined,
      actionType: actionType && actionType.length > 0 ? actionType.join(',') : undefined,
      result: result && result.length > 0 ? result.join(',') : undefined,
      from,
      to,
      page,
      pageSize,
    },
  })
  return data
}
