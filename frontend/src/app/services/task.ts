import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { TaskResponse } from '../models/task.model';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private apiUrl = 'https://localhost:7113/api/tasks'; 

  constructor(private http: HttpClient) {}

  getTasks() {
    return this.http.get<TaskResponse[]>(this.apiUrl);
  }

  updateTask(id: number, dto: any) {
    return this.http.put<TaskResponse>(`${this.apiUrl}/${id}`, dto).pipe(
      catchError((error: HttpErrorResponse) => {
        const msg = error.error?.message || 'Erro ao processar tarefa';
        alert(msg);
        return throwError(() => new Error(msg));
      })
    );
  }

  deleteTask(id: number) {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
export class Task {
  
}