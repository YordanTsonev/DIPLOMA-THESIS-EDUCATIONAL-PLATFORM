import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { describeError } from '../../core/auth/problem-details';
import { ContentService, Material, MaterialFolder } from '../../core/content/content.service';
import { TeachingAssignment } from '../../core/school/school.models';

@Component({
  selector: 'app-materials',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DatePipe, DecimalPipe],
  styleUrl: './materials.scss',
  templateUrl: './materials.html',
})
export class MaterialsComponent implements OnInit {
  private readonly content = inject(ContentService);
  private readonly auth = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly assignments = signal<readonly TeachingAssignment[]>([]);
  protected readonly folders = signal<readonly MaterialFolder[]>([]);
  protected readonly materials = signal<readonly Material[]>([]);
  protected readonly selectedAssignmentId = signal<string>('');
  protected readonly loading = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly pickedFile = signal<File | null>(null);

  protected readonly canManage = computed(() => {
    const role = this.auth.role();
    return role === 'Teacher' || role === 'Admin';
  });

  protected readonly visibleFolders = computed(() =>
    this.folders().filter((folder) => folder.teachingAssignmentId === this.selectedAssignmentId()),
  );

  protected readonly visibleMaterials = computed(() =>
    this.materials().filter((item) => item.teachingAssignmentId === this.selectedAssignmentId()),
  );

  protected readonly uploadForm = this.formBuilder.nonNullable.group({
    title: [''],
    folderId: [''],
  });

  protected readonly linkForm = this.formBuilder.nonNullable.group({
    url: ['', [Validators.required]],
    title: [''],
  });

  protected readonly folderForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
  });

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const view = await this.content.list();
      this.assignments.set(view.assignments);
      this.folders.set(view.folders);
      this.materials.set(view.materials);

      if (!this.selectedAssignmentId() && view.assignments.length > 0) {
        this.selectedAssignmentId.set(view.assignments[0].id);
      }
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected selectAssignment(id: string): void {
    this.selectedAssignmentId.set(id);
  }

  protected onFilePicked(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.pickedFile.set(input.files?.item(0) ?? null);
  }

  protected folderName(folderId: string | null): string | null {
    return this.folders().find((folder) => folder.id === folderId)?.name ?? null;
  }

  protected async upload(): Promise<void> {
    const file = this.pickedFile();
    const assignmentId = this.selectedAssignmentId();

    if (!file || !assignmentId || this.busy()) {
      return;
    }

    await this.run(async () => {
      const { title, folderId } = this.uploadForm.getRawValue();
      await this.content.upload(assignmentId, file, title.trim() || file.name, folderId || null);
      this.uploadForm.reset();
      this.pickedFile.set(null);
      this.notice.set('Материалът е качен.');
    });
  }

  protected async addLink(): Promise<void> {
    const assignmentId = this.selectedAssignmentId();

    if (this.linkForm.invalid || !assignmentId || this.busy()) {
      this.linkForm.markAllAsTouched();
      return;
    }

    await this.run(async () => {
      const { url, title } = this.linkForm.getRawValue();
      await this.content.addLink(assignmentId, url, title.trim() || url, null);
      this.linkForm.reset();
      this.notice.set('Връзката е добавена.');
    });
  }

  protected async createFolder(): Promise<void> {
    const assignmentId = this.selectedAssignmentId();

    if (this.folderForm.invalid || !assignmentId || this.busy()) {
      this.folderForm.markAllAsTouched();
      return;
    }

    await this.run(async () => {
      await this.content.createFolder(assignmentId, this.folderForm.getRawValue().name);
      this.folderForm.reset();
      this.notice.set('Папката е създадена.');
    });
  }

  protected async remove(material: Material): Promise<void> {
    await this.run(async () => {
      await this.content.remove(material.id);
      this.notice.set('Материалът е премахнат.');
    });
  }

  protected async download(material: Material): Promise<void> {
    this.error.set(null);
    try {
      await this.content.download(material);
    } catch (error) {
      this.error.set(describeError(error));
    }
  }

  private async run(action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    this.notice.set(null);

    try {
      await action();
      await this.load();
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.busy.set(false);
    }
  }
}
