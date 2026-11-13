// Friendly names for the generated schema types, so features never reach into `components["schemas"]`.
import type { components } from './schema'

export type Verdict = components['schemas']['Verdict']
export type ScanMode = components['schemas']['ScanMode']
export type Confidence = components['schemas']['Confidence']
export type ScanResponse = components['schemas']['ScanResponse']
export type Finding = components['schemas']['Finding']
export type FindingKind = components['schemas']['FindingKind']
export type SourceStatus = components['schemas']['SourceStatus']
export type SourceState = components['schemas']['SourceState']
export type ThreatSource = components['schemas']['ThreatSource']
export type HistoryResponse = components['schemas']['HistoryResponse']
export type ScanHistoryEntry = components['schemas']['ScanHistoryEntry']
export type StatsResponse = components['schemas']['StatsResponse']
export type VerdictCounts = components['schemas']['VerdictCounts']
export type HostStatistics = components['schemas']['HostStatistics']
