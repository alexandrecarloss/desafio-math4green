import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { TaskService } from '../../services/task.service';
import { TaskResponse, WorkStatus } from '../../models/task.model';
import { JsonPipe, CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  DragDropModule,
  CdkDragDrop,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import { User } from '../../models/user.model';

interface Toast {
  message: string;
  type: 'error' | 'success';
}

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [JsonPipe, CommonModule, FormsModule, DragDropModule],
  templateUrl: './task-list.html',
  styleUrls: ['./task-list.css'],
})
export class TaskListComponent implements OnInit {
  tasks: TaskResponse[] = [];
  users: User[] = [];
  selectedTask: TaskResponse | null = null;
  showModal = false;
  newTaskTitle: string = '';
  pendingTasks: TaskResponse[] = [];
  inProgressTasks: TaskResponse[] = [];
  doneTasks: TaskResponse[] = [];
  notifications: Toast[] = [];
  showConfirmModal = false;
  taskIdToDelete: number | null = null;
  taskTitleToDelete: string = '';
  isLoading = false;
  isSaving = false;

  constructor(private taskService: TaskService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadTasks();
    this.loadUsers();
  }

  showToast(message: string, type: 'error' | 'success' = 'error') {
    const toast: Toast = { message, type };
    this.notifications.push(toast);
    setTimeout(() => {
      const index = this.notifications.indexOf(toast);
      if (index > -1) {
        this.notifications.splice(index, 1);
        this.cdr.detectChanges();
      }
    }, 4000);
  }

  private extractErrorMessage(err: any): string {
    if (!err) return 'Erro inesperado';
    if (typeof err.error === 'string') return err.error;
    if (err.error?.message) return err.error.message;
    if (err.message) return err.message;
    return 'Erro ao processar requisição';
  }

  private handleHttpError(err: any) {
    const msg = this.extractErrorMessage(err);
    this.showToast(msg, 'error');
    this.loadTasks();
  }

  loadUsers() {
    this.taskService.getUsers().subscribe((data) => (this.users = data));
  }

  loadTasks() {
    this.isLoading = true;
    this.taskService.getTasks().subscribe({
      next: (data) => {
        this.tasks = data;
        this.filterTasks();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.handleHttpError(err);
        this.isLoading = false;
      },
    });
  }

  filterTasks() {
    this.pendingTasks = this.tasks.filter((t) => t.status === 'Pending');
    this.inProgressTasks = this.tasks.filter((t) => t.status === 'InProgress');
    this.doneTasks = this.tasks.filter((t) => t.status === 'Done');
  }

  openEditModal(task: TaskResponse) {
    const currentIds = this.tasks
      .filter((t) => task.prerequisiteTitles?.includes(t.title))
      .map((t) => t.id);

    this.selectedTask = { ...task, prerequisiteIds: currentIds };
    this.showModal = true;
  }

  saveTask() {
    if (!this.selectedTask || this.isSaving) return;

    if (
      !this.selectedTask.assignedUserId &&
      (this.selectedTask.status === 'InProgress' || this.selectedTask.status === 'Done')
    ) {
      this.showToast('Uma tarefa em andamento ou concluída deve ter um responsável.', 'error');
      return;
    }

    this.isSaving = true;
    let pIds = (this.selectedTask.prerequisiteIds || []).map((id) => Number(id));

    const dto = {
      title: this.selectedTask.title,
      assignedUserId: this.selectedTask.assignedUserId
        ? Number(this.selectedTask.assignedUserId)
        : null,
      status: this.selectedTask.status,
    };

    this.taskService.updateTask(this.selectedTask.id, dto).subscribe({
      next: () => {
        this.taskService.syncDependencies(this.selectedTask!.id, pIds).subscribe({
          next: () => {
            this.isSaving = false;
            this.finalizeSave();
          },
          error: (err) => {
            this.isSaving = false;
            this.showToast(this.extractErrorMessage(err), 'error');
          },
        });
      },
      error: (err) => {
        this.isSaving = false;
        this.showToast(this.extractErrorMessage(err), 'error');
      },
    });
  }

  private finalizeSave() {
    this.showModal = false;
    this.loadTasks();
    this.showToast('Tarefa e dependências atualizadas!', 'success');
  }

  changeStatus(task: TaskResponse, newStatus: WorkStatus) {
    if (newStatus !== 'Pending' && this.isTaskBlockedByTitles(task)) {
      const blockers = this.getMissingPrerequisites(task).join(', ');
      this.showToast(`Bloqueada! Conclua primeiro: ${blockers}`, 'error');
      this.loadTasks();
      return;
    }
    const dto = {
      status: newStatus,
      assignedUserId: task.assignedUserId,
    };

    this.taskService.updateTask(task.id, dto).subscribe({
      next: () => {
        this.showToast('Status atualizado!', 'success');
        this.loadTasks();
      },
      error: (err) => this.handleHttpError(err),
    });
  }

  getMissingPrerequisites(task: TaskResponse): string[] {
    if (!task.prerequisiteTitles) return [];
    return task.prerequisiteTitles.filter((title) => {
      const preTask = this.tasks.find((t) => t.title === title);
      return !preTask || preTask.status !== 'Done';
    });
  }

  isTaskBlockedByTitles(task: TaskResponse): boolean {
    return this.getMissingPrerequisites(task).length > 0;
  }

  isCircularDependency(candidateId: number): boolean {
    if (!this.selectedTask) return false;
    const candidate = this.tasks.find((t) => t.id === candidateId);
    return candidate?.prerequisiteTitles?.includes(this.selectedTask.title) || false;
  }

  toggleDependency(id: number) {
    if (!this.selectedTask) return;
    this.selectedTask.prerequisiteIds = this.selectedTask.prerequisiteIds || [];
    const index = this.selectedTask.prerequisiteIds.indexOf(id);
    index >= 0
      ? this.selectedTask.prerequisiteIds.splice(index, 1)
      : this.selectedTask.prerequisiteIds.push(id);
  }

  createTask() {
    if (!this.newTaskTitle.trim()) return;
    this.taskService.createTask({ title: this.newTaskTitle }).subscribe(() => {
      this.newTaskTitle = '';
      this.loadTasks();
    });
  }

  deleteTask(id: number) {
    const taskToDelete = this.tasks.find((t) => t.id === id);
    if (!taskToDelete) return;

    const dependentTasks = this.tasks
      .filter((t) => t.prerequisiteTitles?.includes(taskToDelete.title))
      .map((t) => t.title);

    if (dependentTasks.length > 0) {
      this.showToast(
        `Erro: Esta tarefa é pré-requisito de: [${dependentTasks.join(', ')}]`,
        'error'
      );
      return;
    }
    this.taskIdToDelete = id;
    this.taskTitleToDelete = taskToDelete.title;
    this.showConfirmModal = true;
  }

  confirmDeletion() {
    if (this.taskIdToDelete) {
      this.taskService.deleteTask(this.taskIdToDelete).subscribe({
        next: () => {
          this.showToast(`Tarefa removida`, 'success');
          this.showConfirmModal = false;
          this.showModal = false;
          this.loadTasks();
        },
        error: (err) => this.showToast(this.extractErrorMessage(err), 'error'),
      });
    }
  }

  onDrop(event: CdkDragDrop<TaskResponse[]>, newStatus: WorkStatus) {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      const task = event.previousContainer.data[event.previousIndex];
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
      this.changeStatus(task, newStatus);
    }
  }

  clearDependencies() {
    if (this.selectedTask) {
      this.selectedTask.prerequisiteIds = [];
    }
  }
}
