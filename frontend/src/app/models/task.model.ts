export type WorkStatus = 'Pending' | 'InProgress' | 'Done';

export interface TaskResponse {
  id: number;
  title: string;
  status: WorkStatus;
  isBlocked: boolean;

  assignedUserId?: number;
  assignedUserName?: string;

  prerequisiteIds: number[];
  prerequisiteTitles: string[];
}
