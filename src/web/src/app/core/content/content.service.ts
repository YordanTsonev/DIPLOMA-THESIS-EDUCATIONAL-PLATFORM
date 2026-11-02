import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TeachingAssignment } from '../school/school.models';

export interface MaterialFolder {
  readonly id: string;
  readonly teachingAssignmentId: string;
  readonly name: string;
}

export interface Material {
  readonly id: string;
  readonly teachingAssignmentId: string;
  readonly folderId: string | null;
  readonly kind: string;
  readonly title: string;
  readonly description: string | null;
  readonly fileName: string | null;
  readonly contentType: string | null;
  readonly sizeBytes: number | null;
  readonly url: string | null;
  readonly createdAt: string;
}

export interface MaterialsView {
  readonly assignments: readonly TeachingAssignment[];
  readonly folders: readonly MaterialFolder[];
  readonly materials: readonly Material[];
}

@Injectable({ providedIn: 'root' })
export class ContentService {
  private readonly http = inject(HttpClient);

  list(teachingAssignmentId?: string): Promise<MaterialsView> {
    let params = new HttpParams();
    if (teachingAssignmentId) {
      params = params.set('teachingAssignmentId', teachingAssignmentId);
    }
    return firstValueFrom(this.http.get<MaterialsView>('/api/v1/materials', { params }));
  }

  upload(
    teachingAssignmentId: string,
    file: File,
    title: string,
    folderId: string | null,
  ): Promise<Material> {
    const form = new FormData();
    form.append('teachingAssignmentId', teachingAssignmentId);
    form.append('file', file);
    form.append('title', title);
    if (folderId) {
      form.append('folderId', folderId);
    }
    return firstValueFrom(this.http.post<Material>('/api/v1/materials/upload', form));
  }

  addLink(
    teachingAssignmentId: string,
    url: string,
    title: string,
    folderId: string | null,
  ): Promise<Material> {
    return firstValueFrom(
      this.http.post<Material>('/api/v1/materials/links', {
        teachingAssignmentId,
        url,
        title,
        folderId,
        description: null,
      }),
    );
  }

  createFolder(teachingAssignmentId: string, name: string): Promise<MaterialFolder> {
    return firstValueFrom(
      this.http.post<MaterialFolder>('/api/v1/materials/folders', { teachingAssignmentId, name }),
    );
  }

  remove(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/materials/${id}`));
  }

  async download(material: Material): Promise<void> {
    const blob = await firstValueFrom(
      this.http.get(`/api/v1/materials/${material.id}/download`, { responseType: 'blob' }),
    );

    const href = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = href;
    anchor.download = material.fileName ?? material.title;
    anchor.click();
    URL.revokeObjectURL(href);
  }
}
