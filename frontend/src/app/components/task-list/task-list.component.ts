import { Component, OnInit } from '@angular/core';
import { TaskService } from '../../services/task.service';
import { TaskResponse, WorkStatus } from '../../models/task.model';
import { JsonPipe, CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DragDropModule, CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { ChangeDetectorRef } from '@angular/core';
import { User } from '../../models/user.model';
interface Toast { message: string; type: 'error' | 'success' }

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
  constructor(private taskService: TaskService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadTasks();
    this.loadUsers();
  }

  loadUsers() {
    this.taskService.getUsers().subscribe(data => this.users = data);
  }

  openEditModal(task: TaskResponse) {
    const currentIds = this.tasks
      .filter(t => task.prerequisiteTitles?.includes(t.title))
      .map(t => t.id);

    this.selectedTask = { 
      ...task, 
      prerequisiteIds: currentIds 
    };
    this.showModal = true;
  }
  
  loadTasks() {
    this.taskService.getTasks().subscribe((data) => {
      this.tasks = data;
      this.filterTasks();
      this.cdr.detectChanges();
    });
  }

  filterTasks() {
    this.pendingTasks = this.tasks.filter(t => t.status === 'Pending');
    this.inProgressTasks = this.tasks.filter(t => t.status === 'InProgress');
    this.doneTasks = this.tasks.filter(t => t.status === 'Done');
  }
  
  saveTask() {
    if (!this.selectedTask) return;
    
    if (!this.selectedTask.assignedUserId || this.selectedTask.assignedUserId === null) {
      if (this.selectedTask.status === 'InProgress' || this.selectedTask.status === 'Done') {
        this.showToast("Uma tarefa em andamento ou concluída deve ter um responsável.", "error");
        return;
      }
    }

    if (this.selectedTask.status === 'InProgress' && this.selectedTask.assignedUserId) {
      const userAlreadyBusy = this.inProgressTasks.some(t => 
        t.assignedUserId === Number(this.selectedTask!.assignedUserId) && 
        t.id !== this.selectedTask!.id
      );

      if (userAlreadyBusy) {
        this.showToast("Este usuário já possui uma tarefa em andamento.", "error");
        return;
      }
    }

    let pIds = (this.selectedTask.prerequisiteIds || []).map(id => Number(id));
    if (pIds.includes(0)) pIds = [];

    const dto = {
      title: this.selectedTask.title,
      assignedUserId: this.selectedTask.assignedUserId ? Number(this.selectedTask.assignedUserId) : null,
      status: this.selectedTask.status
    };

    this.taskService.updateTask(this.selectedTask.id, dto).subscribe({
      next: () => {
        this.taskService.syncDependencies(this.selectedTask!.id, pIds).subscribe({
          next: () => {
            this.finalizeSave(); 
          },
          error: (err) => {
            this.showToast(err.error?.message || "Erro de dependência", "error");
          }
        });
      },
      error: (err) => {
        this.showToast(err.error?.message || "Erro ao salvar tarefa", "error");
      }
    });
  }

  private finalizeSave() {
    this.showModal = false;
    this.loadTasks();
    this.showToast("Tarefa e dependências atualizadas!", "success");
  }

  getTasksByStatus(status: string) {
    return this.tasks.filter((t) => t.status === status);
  }

 changeStatus(task: TaskResponse, newStatus: WorkStatus) {
    if (task.isBlocked && newStatus !== 'Pending') {
      this.showToast("Esta tarefa está bloqueada por dependências.", 'error');
      return;
    }

    this.taskService.updateTask(task.id, { ...task, status: newStatus }).subscribe({
      next: () => {
        this.showToast("Status atualizado!", "success");
        this.loadTasks();
      },
      error: (err) => {
        const errorMessage = err.message || "Erro ao validar regras";
        this.showToast(errorMessage, "error");
        this.loadTasks();
      }
    });
  }

  getMissingPrerequisites(task: TaskResponse): string[] {
    if (!task.prerequisiteTitles || task.prerequisiteTitles.length === 0) return [];
    
    return task.prerequisiteTitles.filter(title => {
      const preTask = this.tasks.find(t => t.title === title);
      return !preTask || preTask.status !== 'Done';
    });
  }

  createTask() {
    if (!this.newTaskTitle.trim()) return;
    this.taskService.createTask({ title: this.newTaskTitle }).subscribe(() => {
      this.newTaskTitle = '';
      this.loadTasks();
    });
  }

  deleteTask(id: number) {
    const taskToDelete = this.tasks.find(t => t.id === id);
    if (!taskToDelete) return;

    const isPrerequisite = this.tasks.some(t => 
      t.prerequisiteTitles?.includes(taskToDelete.title)
    );

    if (isPrerequisite) {
      const dependentTasks = this.tasks
        .filter(t => t.prerequisiteTitles?.includes(taskToDelete.title))
        .map(t => t.title)
        .join(', ');

      this.showToast(`Erro: Esta tarefa é pré-requisito de: [${dependentTasks}]`, 'error');
      return;
    }
    this.taskIdToDelete = id;
    this.taskTitleToDelete = taskToDelete.title;
    this.showConfirmModal = true;
  }

  confirmDeletion() {
    if (this.taskIdToDelete !== null) {
      this.taskService.deleteTask(this.taskIdToDelete).subscribe({
        next: () => {
          this.showToast(`Tarefa removida com sucesso`, "success");
          this.showConfirmModal = false;
          this.showModal = false;
          this.loadTasks();
        },
        error: () => this.showToast("Erro ao excluir no servidor", "error")
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


  isTaskBlockedByTitles(task: TaskResponse): boolean {
    if (!task.prerequisiteTitles || task.prerequisiteTitles.length === 0) {
      return false;
    }
    return task.prerequisiteTitles.some(preTitle => {
      const preTask = this.tasks.find(t => t.title === preTitle);
      return preTask && preTask.status !== 'Done';
    });
  }

  isCircularDependency(candidateId: number): boolean {
    if (!this.selectedTask) return false;
    
    const candidate = this.tasks.find(t => t.id === candidateId);
    if (!candidate) return false;

    return candidate.prerequisiteTitles?.includes(this.selectedTask.title) || false;
  }

  toggleDependency(id: number) {
    if (!this.selectedTask) return;

    if (!this.selectedTask.prerequisiteIds) {
      this.selectedTask.prerequisiteIds = [];
    }

    const index = this.selectedTask.prerequisiteIds.indexOf(id);

    if (index >= 0) {
      this.selectedTask.prerequisiteIds.splice(index, 1);
    } else {
      this.selectedTask.prerequisiteIds.push(id);
    }
  }

  clearDependencies() {
    if (this.selectedTask) {
      this.selectedTask.prerequisiteIds = [];
    }
  }

}