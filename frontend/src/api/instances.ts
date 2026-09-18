import client from './client'

export interface Instance {
  instanceId: string
  name: string
  state: string
  dnsEnabled: boolean
  publicIp: string | null
  privateIp: string | null
  region: string
  accountKey: string
  launchTime: string | null
  instanceType: string | null
}

export interface ListInstancesParams {
  accountKey: string
  region?: string
  statuses?: string
  search?: string
  dnsOnly?: boolean
}

export async function listInstances(params: ListInstancesParams): Promise<Instance[]> {
  const { data } = await client.get<Instance[]>('/instances', { params })
  return data
}

export interface SkippedInstance {
  instanceId: string
  reason: string
}

export interface InstanceActionResult {
  affected: string[]
  skipped: SkippedInstance[]
  errors: string[]
  dryRun: boolean
}

export async function startInstances(accountKey: string, region: string, instanceIds: string[], dryRun: boolean): Promise<InstanceActionResult> {
  const { data } = await client.post<InstanceActionResult>('/instances/start', { accountKey, region, instanceIds, dryRun })
  return data
}

export async function stopInstances(accountKey: string, region: string, instanceIds: string[], dryRun: boolean): Promise<InstanceActionResult> {
  const { data } = await client.post<InstanceActionResult>('/instances/stop', { accountKey, region, instanceIds, dryRun })
  return data
}
