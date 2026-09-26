# Plan: Issue 50 - M1's acceptance criterion has never been measured - real microphone, multi-accent recognition

## Exhaustive Outline
The first milestone requires the game to understand spoken words accurately and quickly. Specifically, it needs to correctly identify spoken words from a specific list of 40 words at least 90 percent of the time. This must be tested across four different accents using a real microphone. Furthermore, the game must react to the spoken word within 150 milliseconds. Currently, we have not actually performed this test to see if we meet the goal. This task is to conduct the test, measure the accuracy and speed, and record the results in our project documentation.

## Step by Step Execution Instructions

1.  **Prepare the Test Environment:**
    Set up a real microphone and ensure the game is running with the current 40-word dictionary.
2.  **Gather Test Subjects:**
    Find people with at least four distinct accents to participate in the test.
3.  **Conduct the Voice Test:**
    Have each person speak the 40 words into the microphone while the game is running.
4.  **Measure Accuracy and Speed:**
    Record how often the game correctly identifies the spoken word. Also record the time it takes for the game to react after the word is spoken.
5.  **Update the Project Documentation:**
    Open the docs/3-state/ProjectState.md file. Add a new section detailing the results of the microphone test, including the recognition rate and the latency measurements. Note whether the results meet the milestone requirements.

## Verification Steps
1.  Read the docs/3-state/ProjectState.md file to confirm that the test results are clearly documented.
2.  Verify that the documented results include both the recognition accuracy percentage and the reaction time in milliseconds.
3.  Ensure the document lists the four accents that were tested.

## Completion Checks
*   [ ] A real-microphone test was run across at least four distinct accents against the 40-word dictionary.
*   [ ] The top-1 recognition rate was measured and recorded.
*   [ ] The word-end-to-effect latency was measured and recorded.
*   [ ] The results are written up against the M1 acceptance criterion in docs/3-state/ProjectState.md.


## Technical Constraints
When executing this plan, you MUST read and strictly adhere to ALL principles and conventions detailed in `docs/UnityConvention.md`. 
You cannot pick and choose which rules to enforce; every single rule applies.
Specifically, you must follow:
- Core Architectural Principles: KISS, YAGNI, Solve the Root Cause, DRY, and SRP.
- All Naming Conventions (e.g., `m_camelCase` for privates, `s_camelCase` for statics, `PascalCase` for methods/properties).
- All Formatting & Syntax rules (e.g., Allman braces, mandatory braces, 4-space indentation).
- Class & Method Organization (Newspaper metaphor, correct layout order).
- Unity-Specific Implementations (e.g., `[SerializeField]` instead of public, `[Tooltip]` instead of comments).
- UI Toolkit (UXML/USS) Naming (BEM convention, kebab-case).
- Commenting rules (Explain 'Why', not 'What').
