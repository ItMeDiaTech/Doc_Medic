# Doc_Medic Test Suite

This test suite provides comprehensive testing coverage for the Doc_Medic application following CLAUDE.md Section 16 specifications.

## Test Structure

```
tests/App.Tests/
├── Unit/                   # Unit tests for core functionality
├── Integration/            # End-to-end pipeline tests
├── Fixtures/               # Test document builders and sample files
├── TestHelpers/           # Test utilities and builders
└── README.md              # This file
```

## Test Categories

### 1. Unit Tests (`/Unit`)

#### HyperlinkPatternsTests.cs
- **Purpose**: Tests regex patterns and URL processing logic
- **Coverage**:
  - Document_ID extraction with delimiters (&, #), URL-encoded values
  - Content_ID pattern matching: `CMS-1-123456`, `TSRC-asd2121jkla-123456`
  - URL eligibility determination
  - Display text suffix handling (6/5 digits, zero-padding)
  - Canonical URL building
  - Space normalization patterns
  - Top of Document text recognition
  - Case sensitivity/insensitivity rules
  - Edge cases and error conditions

#### HyperlinkRepairTests.cs
- **Purpose**: Tests hyperlink repair with multi-run display text scenarios
- **Coverage**:
  - TextRange helper for safe display text updates
  - Multi-run hyperlink display text extraction and concatenation
  - Content_ID suffix insertion logic (XXXXXX format)
  - Display text formatting preservation across runs
  - URL eligibility checking and canonical URL updates
  - Hyperlink relationship management

#### StyleStandardizationTests.cs
- **Purpose**: Tests OpenXML style property verification
- **Coverage**:
  - Normal style: Verdana, 12pt, black, 6pt before spacing
  - Heading 1: Verdana, 18pt, bold, 0pt before/12pt after
  - Heading 2: Verdana, 14pt, bold, 6pt before/6pt after  
  - Hyperlink character style: Verdana, 12pt, blue #0000FF, underline
  - Font size conversion (points to half-points)
  - Spacing conversion (points to twips)
  - Color validation (hex format)
  - Style application to paragraphs
  - StylesPart creation when missing

#### TopOfDocumentTests.cs
- **Purpose**: Tests bookmark creation and internal anchor functionality
- **Coverage**:
  - DocStart bookmark creation at document start
  - BookmarkStart/BookmarkEnd ID matching
  - Top of Document pattern recognition (case-insensitive)
  - Internal hyperlink anchor linking
  - Right-alignment of Top of Document paragraphs
  - Hyperlink character style application
  - Whitespace normalization in link text
  - Multiple Top of Document links handling
  - Unique bookmark ID management

#### LookupServiceTests.cs
- **Purpose**: Tests Power Automate API client with HTTP mocking
- **Coverage**:
  - HTTP request format validation
  - JSON request/response serialization
  - Error handling (HTTP errors, timeouts, invalid JSON)
  - Retry policy behavior
  - Cancellation token support
  - Response field mapping (Title, Status, Content_ID, Document_ID)
  - Large dataset handling performance
  - Network timeout scenarios

### 2. Integration Tests (`/Integration`)

#### ApprovalTests.cs  
- **Purpose**: Golden file approval tests for XML transforms
- **Coverage**:
  - Document structure comparison before/after processing
  - Styles XML verification against approved baselines
  - Hyperlink relationships XML validation
  - Text content verification with Content_ID suffixes
  - Full pipeline processing tests
  - Idempotent operation verification
  - Regression testing against approved outputs

### 3. Test Fixtures (`/Fixtures`)

#### TestFixtureBuilder.cs
- **Purpose**: Creates sample .docx test documents
- **Available Fixtures**:
  - `sample-eligible-hyperlinks.docx`: Document_ID and Content_ID patterns
  - `sample-multi-run-hyperlinks.docx`: Multi-run display text scenarios
  - `sample-styles-to-standardize.docx`: Style standardization testing
  - `sample-top-of-document.docx`: Top of Document link scenarios
  - `sample-formatting-issues.docx`: Space normalization and image centering
  - `comprehensive-test-document.docx`: All processing scenarios combined
  - `minimal-test-document.docx`: Basic validation scenarios

#### Expected Output Documents
- Corresponding expected output files for approval testing
- Represents processed documents after Doc_Medic transformations

### 4. Test Helpers (`/TestHelpers`)

#### DocumentBuilder.cs
- **Purpose**: Fluent API for creating OpenXML test documents
- **Features**:
  - Add paragraphs with styles
  - Create hyperlinks (external and internal)
  - Multi-run hyperlink creation for TextRange testing
  - Bookmark management
  - Style definition and property setting
  - Image insertion with alignment
  - Document building and stream management

#### Static Helper Classes
- `TestDocuments`: Pre-configured document scenarios
- `FixtureFiles`: Test file path management and fixture creation

## Test Execution

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "FullyQualifiedName~Unit"
dotnet test --filter "FullyQualifiedName~Integration"

# Run specific test class
dotnet test --filter "FullyQualifiedName~HyperlinkPatternsTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Dependencies

- **xUnit**: Primary testing framework
- **FluentAssertions**: Assertion library for readable tests
- **Moq**: Mocking framework for dependencies
- **ApprovalTests**: Golden file comparison testing
- **DocumentFormat.OpenXml**: OpenXML document manipulation
- **Microsoft.AspNetCore.Mvc.Testing**: HTTP client testing utilities

## Key Test Scenarios

### Regex Pattern Validation
- Document_ID extraction: `?docid=12345&param=value` → `12345`
- Content_ID extraction: `CMS-Project1-123456` → `CMS-Project1-123456`
- URL-encoded values: `?docid=doc%20with%20spaces`
- Delimiter handling: `#`, `&`, end-of-string
- Case sensitivity: Document_ID (insensitive), Content_ID (sensitive)

### Hyperlink Processing
- Multi-run text: `["Document", " Title", " Split"]` → `"Document Title Split (123456)"`
- Canonical URL: `http://old.com?docid=123` → `http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=123`
- Display suffix: `"Link Text"` → `"Link Text (123456)"`
- Zero-padding: 5 digits → 6 digits with leading zero

### Style Standardization
- Font: All text uses Verdana
- Sizes: Normal (12pt), Heading1 (18pt), Heading2 (14pt)
- Colors: Normal/Headings (black), Hyperlinks (blue #0000FF)
- Spacing: Normal (6pt before), Heading1 (0pt before, 12pt after), Heading2 (6pt before/after)

### Power Automate API
- Request format: `{ "Lookup_IDs": ["DOC-123", "CMS-1-456"] }`
- Response mapping: `Title`, `Status`, `Content_ID`, `Document_ID`
- Error handling: HTTP errors, JSON parsing, timeouts
- Retry policy: Transient failures with exponential backoff

## Approval Testing Workflow

1. **Initial Run**: Tests create `.received.txt` files with actual output
2. **Review**: Compare `.received.txt` with expected results  
3. **Approve**: Rename `.received.txt` to `.approved.txt` if correct
4. **Future Runs**: Tests compare output against `.approved.txt` files
5. **Changes**: Update approved files when intentional changes are made

## Test Data Management

### Sample Documents
- Created programmatically using `DocumentBuilder`
- Consistent test scenarios across test runs
- Isolated test data (no shared state)

### Mock API Responses
- Configurable HTTP responses for different scenarios
- Error simulation (timeouts, HTTP errors, invalid JSON)
- Sequence testing (retry scenarios)

### Fixture Files
- Generated on-demand in `Fixtures/Documents/` directory
- Cleaned up after test runs
- Available for manual inspection during development

## Coverage Goals

- **Unit Tests**: 90%+ code coverage for core logic
- **Integration Tests**: End-to-end pipeline validation
- **Edge Cases**: Error conditions, malformed inputs, boundary values
- **Performance**: Large document handling, API timeout scenarios
- **Regression**: Prevent breaking changes to approved outputs

## Debugging Tests

### Common Issues
1. **Package Restore**: Ensure NuGet.Config allows nuget.org access
2. **File Locks**: Dispose DocumentBuilder instances properly
3. **Path Issues**: Use absolute paths in test assertions
4. **Approval Mismatches**: Check line endings and whitespace normalization

### Test Isolation
- Each test class manages its own disposables
- Temporary files cleaned up after tests
- No shared state between test methods
- Independent HTTP client instances for API tests