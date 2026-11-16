using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Assessment.Domain;

public enum QuestionKind
{
    SingleChoice = 1,
    MultipleChoice = 2,
    OpenText = 3,
}

public sealed class QuestionOption : Entity
{
    private QuestionOption(Guid id, string text, bool isCorrect, int position)
        : base(id)
    {
        Text = text;
        IsCorrect = isCorrect;
        Position = position;
    }

    private QuestionOption() { }

    public string Text { get; private set; } = null!;

    public bool IsCorrect { get; private set; }

    public int Position { get; private set; }

    internal static QuestionOption Create(string? text, bool isCorrect, int position)
    {
        var trimmed = text?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException("An answer option cannot be empty.");
        }

        if (trimmed.Length > Question.MaxOptionLength)
        {
            throw new DomainException($"An option must not exceed {Question.MaxOptionLength} characters.");
        }

        return new QuestionOption(Guid.CreateVersion7(), trimmed, isCorrect, position);
    }
}

public sealed class Question : AggregateRoot
{
    public const int MaxTextLength = 2000;
    public const int MaxOptionLength = 500;
    public const int MaxExplanationLength = 1000;
    public const int MinOptions = 2;
    public const int MaxOptions = 10;

    private readonly List<QuestionOption> _options = [];

    private Question(
        Guid id,
        Guid subjectId,
        QuestionKind kind,
        string text,
        string? explanation,
        Guid authorUserId)
        : base(id)
    {
        SubjectId = subjectId;
        Kind = kind;
        Text = text;
        Explanation = explanation;
        AuthorUserId = authorUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private Question() { }

    public Guid SubjectId { get; private set; }

    public QuestionKind Kind { get; private set; }

    public string Text { get; private set; } = null!;

    public string? Explanation { get; private set; }

    public Guid AuthorUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<QuestionOption> Options => _options;

    public bool IsOpen => Kind == QuestionKind.OpenText;

    public static Question Create(
        Guid subjectId,
        QuestionKind kind,
        string? text,
        string? explanation,
        IReadOnlyList<(string Text, bool IsCorrect)> options,
        Guid authorUserId)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (subjectId == Guid.Empty)
        {
            throw new DomainException("A question must belong to a subject.");
        }

        if (authorUserId == Guid.Empty)
        {
            throw new DomainException("The author of the question is required.");
        }

        if (!Enum.IsDefined(kind))
        {
            throw new DomainException($"'{kind}' is not a known question type.");
        }

        var trimmed = text?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException("A question text is required.");
        }

        if (trimmed.Length > MaxTextLength)
        {
            throw new DomainException($"A question must not exceed {MaxTextLength} characters.");
        }

        var note = explanation?.Trim();

        if (note is { Length: > MaxExplanationLength })
        {
            throw new DomainException($"An explanation must not exceed {MaxExplanationLength} characters.");
        }

        var question = new Question(
            Guid.CreateVersion7(), subjectId, kind, trimmed, note is { Length: 0 } ? null : note, authorUserId);

        question.SetOptions(options);

        return question;
    }

    public void SetOptions(IReadOnlyList<(string Text, bool IsCorrect)> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (Kind == QuestionKind.OpenText)
        {
            if (options.Count > 0)
            {
                throw new DomainException("An open question has no answer options.");
            }

            _options.Clear();
            return;
        }

        if (options.Count < MinOptions)
        {
            throw new DomainException($"A closed question needs at least {MinOptions} options.");
        }

        if (options.Count > MaxOptions)
        {
            throw new DomainException($"A question must not have more than {MaxOptions} options.");
        }

        var correct = options.Count(option => option.IsCorrect);

        if (correct == 0)
        {
            throw new DomainException("At least one option must be correct.");
        }

        if (Kind == QuestionKind.SingleChoice && correct > 1)
        {
            throw new DomainException("A single-choice question must have exactly one correct option.");
        }

        _options.Clear();

        for (var index = 0; index < options.Count; index++)
        {
            _options.Add(QuestionOption.Create(options[index].Text, options[index].IsCorrect, index));
        }
    }

    public bool IsAnsweredCorrectlyBy(IReadOnlyCollection<Guid> selectedOptionIds)
    {
        ArgumentNullException.ThrowIfNull(selectedOptionIds);

        if (Kind == QuestionKind.OpenText)
        {
            return false;
        }

        var expected = _options.Where(option => option.IsCorrect).Select(option => option.Id).ToHashSet();

        return expected.SetEquals(selectedOptionIds);
    }
}
