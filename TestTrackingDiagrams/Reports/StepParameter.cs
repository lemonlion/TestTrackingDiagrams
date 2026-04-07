namespace TestTrackingDiagrams.Reports;

public record StepParameter
{
    public required string Name { get; set; }
    public StepParameterKind Kind { get; set; }
    public InlineParameterValue? InlineValue { get; set; }
    public TabularParameterValue? TabularValue { get; set; }
    public TreeParameterValue? TreeValue { get; set; }
}

public enum StepParameterKind { Inline, Tabular, Tree }

public record InlineParameterValue(string Value, string? Expectation, VerificationStatus Status);

public record TabularParameterValue(TabularColumn[] Columns, TabularRow[] Rows);
public record TabularColumn(string Name, bool IsKey);
public record TabularRow(TableRowType Type, TabularCell[] Values);
public record TabularCell(string Value, string? Expectation, VerificationStatus Status);

public enum VerificationStatus { NotApplicable, Success, Failure, Exception, NotProvided }
public enum TableRowType { Matching, Surplus, Missing }

public record TreeParameterValue(TreeNode Root);
public record TreeNode(string Path, string Node, string Value, string? Expectation, VerificationStatus Status, TreeNode[]? Children);
