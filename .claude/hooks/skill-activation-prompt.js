#!/usr/bin/env node
import fs from 'fs';
import path from 'path';

// Get script directory (works regardless of how script is invoked)
const scriptDir = path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, '$1'));
const projectDir = path.resolve(scriptDir, '..', '..');

// Log file for debugging
const logFile = path.join(scriptDir, 'hook.log');

function log(msg) {
    try {
        const timestamp = new Date().toISOString();
        fs.appendFileSync(logFile, `[${timestamp}] ${msg}\n`);
    } catch (e) {
        // Fallback: write to stderr if log file fails
        console.error(`[LOG] ${msg}`);
    }
}

// Log immediately on script load
log('=== Hook script loaded ===');

async function main() {
    try {
        log('Hook started');
        log(`CLAUDE_PROJECT_DIR: ${process.env.CLAUDE_PROJECT_DIR}`);
        log(`CWD: ${process.cwd()}`);

        // Read input from stdin
        let input = '';
        for await (const chunk of process.stdin) {
            input += chunk;
        }
        log(`Input received: ${input.substring(0, 200)}...`);

        const data = JSON.parse(input);
        const prompt = data.prompt.toLowerCase();
        log(`Prompt: ${prompt.substring(0, 100)}`);

        // Load skill rules (use computed projectDir from script location)
        log(`Project dir: ${projectDir}`);

        const rulesPath = path.join(projectDir, '.claude', 'skills', 'skill-rules.json');
        log(`Rules path: ${rulesPath}`);

        let rules;
        try {
            rules = JSON.parse(fs.readFileSync(rulesPath, 'utf-8'));
            log('Rules loaded successfully');
        } catch (e) {
            log(`Failed to load rules: ${e.message}`);
            process.exit(0);
        }

        if (!rules.workflows) {
            process.exit(0);
        }

        const matchedWorkflows = [];

        // Check each workflow for matches
        for (const [workflowId, workflow] of Object.entries(rules.workflows)) {
            const triggers = workflow.triggers;
            if (!triggers) continue;

            // Keyword matching
            if (triggers.keywords) {
                const keywordMatch = triggers.keywords.some(kw =>
                    prompt.includes(kw.toLowerCase())
                );
                if (keywordMatch) {
                    matchedWorkflows.push({ id: workflowId, workflow, matchType: 'keyword' });
                    continue;
                }
            }

            // Intent pattern matching
            if (triggers.intentPatterns) {
                const intentMatch = triggers.intentPatterns.some(pattern => {
                    const regex = new RegExp(pattern, 'i');
                    return regex.test(prompt);
                });
                if (intentMatch) {
                    matchedWorkflows.push({ id: workflowId, workflow, matchType: 'intent' });
                }
            }
        }

        // Generate output if matches found
        if (matchedWorkflows.length > 0) {
            const priorityOrder = { critical: 0, high: 1, medium: 2, low: 3 };
            matchedWorkflows.sort((a, b) =>
                priorityOrder[a.workflow.priority] - priorityOrder[b.workflow.priority]
            );

            let output = '\n';
            output += '┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓\n';
            output += '┃  🎯 SKILL WORKFLOW ACTIVATION                    ┃\n';
            output += '┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛\n\n';

            const critical = matchedWorkflows.filter(w => w.workflow.priority === 'critical');
            const high = matchedWorkflows.filter(w => w.workflow.priority === 'high');
            const medium = matchedWorkflows.filter(w => w.workflow.priority === 'medium');

            const formatWorkflow = (mw, icon) => {
                let result = `${icon} ${mw.workflow.name}\n`;
                result += `   ${mw.workflow.description}\n`;
                result += `   ┌─────────────────────────────────────────────\n`;
                result += `   │ SEQUENCE:\n`;
                mw.workflow.sequence.forEach((skill, idx) => {
                    const isFirst = idx === 0;
                    const arrow = isFirst ? '▶' : '→';
                    const skillShort = skill.replace('superpowers:', '');
                    result += `   │  ${idx + 1}. ${arrow} ${skillShort}${isFirst ? ' ← START HERE' : ''}\n`;
                });
                result += `   └─────────────────────────────────────────────\n\n`;
                return result;
            };

            if (critical.length > 0) {
                output += '⚠️  CRITICAL WORKFLOW (MUST FOLLOW):\n\n';
                critical.forEach(mw => { output += formatWorkflow(mw, '🔴'); });
            }

            if (high.length > 0) {
                output += '📚 RECOMMENDED WORKFLOW:\n\n';
                high.forEach(mw => { output += formatWorkflow(mw, '🟡'); });
            }

            if (medium.length > 0) {
                output += '💡 SUGGESTED WORKFLOW:\n\n';
                medium.forEach(mw => { output += formatWorkflow(mw, '🟢'); });
            }

            const primaryWorkflow = matchedWorkflows[0];
            const firstSkill = primaryWorkflow.workflow.sequence[0];

            output += '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n';
            output += `⚡ ACTION: Invoke Skill tool with "${firstSkill}"\n`;
            output += '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n';

            console.log(output);
        }

        log('Hook completed successfully');
        process.exit(0);
    } catch (err) {
        log(`Hook error: ${err.message}\n${err.stack}`);
        process.exit(0);
    }
}

main();
