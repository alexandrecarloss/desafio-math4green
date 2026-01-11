import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, Observable, throwError } from 'rxjs';
import { TaskResponse } from '../models/task.model';
import { User } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private apiUrl = 'https://localhost:7141/api/'; 

  constructor(private http: HttpClient) {}
  getTasks(): Observable<TaskResponse[]> {
    return this.http.get<TaskResponse[]>(`${this.apiUrl}tasks/`);
  }

  updateTask(id: number, dto: any) {
    return this.http.put<TaskResponse>(`${this.apiUrl}tasks/${id}`, dto).pipe(
      catchError((error: HttpErrorResponse) => {
        const msg = error.error?.message || 'Erro ao processar tarefa';
        return throwError(() => new Error(msg));
      })
    );
  }

  deleteTask(id: number) {
    return this.http.delete(`${this.apiUrl}tasks/${id}`);
  }

  createTask(dto: { title: string }): Observable<TaskResponse> {
    return this.http.post<TaskResponse>(`${this.apiUrl}tasks`, dto);
  }

  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.apiUrl}user`); 
  }

  syncDependencies(taskId: number, prerequisiteIds: number[]) {
    return this.http.post(`${this.apiUrl}tasks/${taskId}/dependencies/sync`, prerequisiteIds);
  }
}
export class Task {
  
}