import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  AssessmentService,
  QUESTION_KINDS,
  QUESTION_KIND_LABELS,
  Question,
} from '../../core/assessment/assessment.service';
import { describeError } from '../../core/auth/problem-details';
import { SchoolService } from '../../core/school/school.service';
import { TeachingAssignment } from '../../core/school/school.models';

interface DraftOption {
  text: string;
  isCorrect: boolean;
}

@Component({
  selector: 'app-question-bank',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  styleUrl: './question-bank.scss',
  templateUrl: './question-bank.html',
})
export class QuestionBankComponent implements OnInit {
  private readonly assessment = inject(AssessmentService);
  private readonly school = inject(SchoolService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly kinds = QUESTION_KINDS;
  protected readonly kindLabels = QUESTION_KIND_LABELS;

  protected readonly assignments = signal<readonly TeachingAssignment[]>([]);
  protected readonly questions = signal<readonly Question[]>([]);
  protected readonly options = signal<readonly DraftOption[]>([
    { text: '', isCorrect: false },
    { text: '', isCorrect: false },
  ]);

  protected readonly loading = signal(false);
  protected readonly busy = signal(false);
  protected readonly formOpen = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);

  protected readonly subjects = computed(() => {
    const seen = new Map<string, string>();

    for (const assignment of this.assignments()) {
      seen.set(assignment.subjectId, assignment.subjectName);
    }

    return [...seen].map(([id, name]) => ({ id, name }));
  });

  protected readonly form = this.formBuilder.nonNullable.group({
    subjectId: ['', Validators.required],
    kind: ['SingleChoice', Validators.required],
    text: ['', [Validators.required, Validators.maxLength(2000)]],
    explanation: ['', Validators.maxLength(1000)],
  });

  protected readonly filter = this.formBuilder.nonNullable.group({
    subjectId: [''],
    ownOnly: [false],
  });

  async ngOnInit(): Promise<void> {
    this.loading.set(true);

    try {
      this.assignments.set(await this.school.assignments());

      const subjects = this.subjects();
      if (subjects.length > 0) {
        this.form.patchValue({ subjectId: subjects[0].id });
      }

      await this.load();
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected async load(): Promise<void> {
    this.error.set(null);

    try {
      const { subjectId, ownOnly } = this.filter.getRawValue();
      this.questions.set(await this.assessment.questions(subjectId || undefined, ownOnly));
    } catch (error) {
      this.error.set(describeError(error));
    }
  }

  protected toggleForm(): void {
    this.formOpen.set(!this.formOpen());
  }

  protected get isOpenKind(): boolean {
    return this.form.getRawValue().kind === 'OpenText';
  }

  protected addOption(): void {
    this.options.set([...this.options(), { text: '', isCorrect: false }]);
  }

  protected removeOption(index: number): void {
    this.options.set(this.options().filter((_, position) => position !== index));
  }

  protected setOptionText(index: number, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.options.set(
      this.options().map((option, position) =>
        position === index ? { ...option, text: value } : option,
      ),
    );
  }

  protected toggleCorrect(index: number): void {
    const single = this.form.getRawValue().kind === 'SingleChoice';

    this.options.set(
      this.options().map((option, position) => {
        if (position === index) {
          return { ...option, isCorrect: !option.isCorrect };
        }

        return single ? { ...option, isCorrect: false } : option;
      }),
    );
  }

  protected async create(): Promise<void> {
    if (this.form.invalid || this.busy()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const payloadOptions = this.isOpenKind
      ? []
      : this.options().filter((option) => option.text.trim() !== '');

    this.busy.set(true);
    this.error.set(null);
    this.notice.set(null);

    try {
      await this.assessment.createQuestion({
        subjectId: value.subjectId,
        kind: value.kind,
        text: value.text,
        explanation: value.explanation.trim() || null,
        options: payloadOptions.map((option) => ({
          text: option.text.trim(),
          isCorrect: option.isCorrect,
        })),
      });

      this.form.patchValue({ text: '', explanation: '' });
      this.options.set([
        { text: '', isCorrect: false },
        { text: '', isCorrect: false },
      ]);
      this.notice.set('Въпросът е добавен в банката.');
      await this.load();
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.busy.set(false);
    }
  }

  protected async remove(question: Question): Promise<void> {
    this.error.set(null);
    this.notice.set(null);

    try {
      await this.assessment.deleteQuestion(question.id);
      this.notice.set('Въпросът е премахнат.');
      await this.load();
    } catch (error) {
      this.error.set(describeError(error));
    }
  }
}
