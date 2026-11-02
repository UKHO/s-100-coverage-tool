---
applyTo: "**/*Test.cs,**/*Tests.cs"
description: Unit test coding guidelines
---

# Unit Test Conventions

## Framework and Libraries

- Use NUnit 4.x as the test framework. Annotate test classes with `[TestFixture]` and test methods with `[Test]` or `[TestCase]`.
- Use FakeItEasy for all test doubles (fakes, stubs, and mocks). Do not use Moq, NSubstitute, or hand-rolled fakes.
- Use Shouldly for all assertions: `actual.Should().Be(expected)`. Never use NUnit's classic or constraint-model assertion styles (`Assert.AreEqual`, `Assert.That`, `Assert.IsTrue` etc.).

## Test Naming

Follow the three-part naming convention for every test method:

- **MethodUnderTest** — the name of the method or property being exercised.
- **Scenario** — a short description of the conditions in place for this test.
- **ExpectedResult** — what the code should do under those conditions.

The three parts are separated by underscores, for example `Constructor_WithNullPrefixAndSuffix_TreatsThemAsEmpty` or `TryParse_WithInputContainingNoDigits_ReturnsFalseAndNullOut`. The full name must read as a plain-English sentence describing the behaviour being verified.

When using `[TestCase]`, supply a `TestName` that follows the same convention so that the test runner output is self-explanatory without reference to the test body.

## Test Structure — Arrange / Act / Assert

Structure every test body around the Arrange / Act / Assert pattern:

- **Arrange** — create the system under test and any dependencies and inputs it needs. Keep this section as small as possible; only configure what the scenario requires.
- **Act** — invoke the method or property under test exactly once. Capture the return value in a clearly named local variable.
- **Assert** — verify the outcome. If multiple assertions concern a single logical result, wrap them in a `using (new AssertionScope()) { ... }` block.

Clearly separate the three sections. When all three phases are present and non-trivial, use `// Arrange`, `// Act`, and `// Assert` comments. When the arrangement is a single obvious line these comments may be omitted for brevity.

## Parameterised Tests

Use `[TestCase]` to express multiple input / expected-output pairs for the same logical scenario. Prefer this over duplicating test methods. Group related `[TestCase]` entries under the same test method; separate logically distinct scenarios into different test methods even if the method signature is identical.

Do not embed loops, conditionals, or string concatenation inside the test body to vary the inputs — use parameterisation instead. If the test logic depends on a condition, split the test into two or more distinct `[TestCase]`-driven methods.

## Use of `[SetUp]` and Shared State

Prefer local variable declarations inside each test over `[SetUp]` methods. Shared setup makes tests harder to read in isolation because the reader must navigate to a different method to understand what state exists. Reserve `[SetUp]` for state that is genuinely identical across every test in the class and that would otherwise produce significant duplication.

Never share mutable state between tests. Each test must be fully independent of every other test; the order in which tests run must not affect any result.

## Assertions and What to Test Through

Test behaviour through public API surface only. Private methods are implementation details; they are tested implicitly through the public members that call them. Do not use reflection or `InternalsVisibleTo` to reach private members. Test through interfaces or abstract base types when that is how the production code exposes the behaviour.

When the subject under test has collaborators (dependencies), use FakeItEasy to create fakes for them. Configure only the return values and interactions that the individual test scenario requires — over-configuring a fake obscures the intent of the test.

## Test Comments and Documentation

Every test that exercises non-obvious behaviour must have a comment explaining *why* the assertion matters, not merely *what* it asserts. A good comment answers: what business or technical rule is this scenario verifying, and why does it matter that the code behaves this way?

On multi-case `[TestCase]` methods, the comment on the method itself should describe the general rule being checked. On individual `[TestCase]` entries, the `TestName` is sufficient documentation unless a particular case has an unusual motivation that deserves a call-out.

Within a test body, write comments above the Arrange section when the test scenario depends on context that is not obvious from the variable names and values alone (for example, explaining a boundary value or a restriction inherited from a known limitation of the underlying implementation).

Comments should be written in complete sentences, using plain present-tense English. They should not restate what the code says; they should explain the reasoning behind the test.

## Minimally Passing Tests

Use only the simplest input values needed to show that the scenario under test behaves correctly. Avoid magic numbers and magic strings — when a specific literal value is necessary, assign it to a clearly named constant or local variable so its purpose is evident. Avoid setting up data that is irrelevant to the current scenario; extra data introduces noise and can mask the intent of the test.

## Infrastructure and Isolation

Unit tests must not depend on infrastructure: no file-system access, no network calls, no database connections, no clocks. Introduce a seam (typically an interface) wherever production code calls an infrastructure concern, then use FakeItEasy to provide a controlled substitute in tests.

Tests must be deterministic: running the same test repeatedly with no changes to the codebase must always produce the same result.

## Boundary and Edge-Case Coverage

For any method that has defined boundaries (ranges, minimum/maximum values, empty collections, null inputs), write dedicated test cases at and immediately outside each boundary. Use `TestName` on each such `[TestCase]` to make the boundary being exercised explicit in the test runner output.

For constructors or methods that must tolerate null or empty arguments without throwing, write an explicit test confirming the coercion or default behaviour. For methods that must throw on null, write an explicit test asserting the exception type using Shouldly: `action.Should().Throw<ArgumentNullException>()`.

## Test Organisation

Place all tests for a class or a logical group of related behaviours in a dedicated `[TestFixture]` class. Use `#region`/`#endregion` blocks to group tests by the member they exercise within a large fixture (for example, `#region Constructor` or `#region LoadProductFiles`). Keep all tests for the same member together inside their region.

## Characteristic Properties of Good Tests

Every test in the suite must satisfy these properties:

- **Fast** — it executes in milliseconds. Anything slower belongs in an integration test.
- **Isolated** — it has no side effects and no dependencies on external state.
- **Repeatable** — it produces the same result on every run regardless of environment or execution order.
- **Self-checking** — it passes or fails automatically without human inspection.
- **Focused** — it verifies a single logical scenario. If adding a new assertion requires a new scenario description, write a new test.