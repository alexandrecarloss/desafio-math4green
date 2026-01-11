export enum WorkStatus {
  Pending = 0,
  InProgress = 1,
  Done = 2
}

export interface TaskResponse {
  id: number;
  title: string;
  status: WorkStatus;
  assignedUserId?: number;
  assignedUserName?: string;
  isBlocked: boolean;
  prerequisiteTitles: string[];
}