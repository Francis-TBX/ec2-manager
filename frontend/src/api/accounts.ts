import client from './client'

export interface Account {
  key: string
  name: string
  accountId: string
}

export async function listAccounts(): Promise<Account[]> {
  const { data } = await client.get<Account[]>('/accounts')
  return data
}
