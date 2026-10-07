import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const css = fs.readFileSync(path.join(__dirname, "styles.css"), "utf8");

function footer(left, page) {
  return `<div class="footer"><span>${left}</span><span class="pg">${page}</span></div>`;
}

function listing(lines, { tint = false, compact = false } = {}) {
  const cls = ["listing", tint ? "tint" : "", compact ? "compact" : ""].filter(Boolean).join(" ");
  return `<div class="${cls}">${lines.map((l) => escapeHtml(l)).join("\n")}</div>`;
}

function escapeHtml(s) {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}

const pages = [];

// PAGE 1 — cover
pages.push(`
<section class="page cover">
  <img src="assets/issue01-cover.jpg" alt="Centauri64 Magazine Issue 1 cover" />
</section>`);

// PAGE 2 — advert
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label black">Advertisement</div>
    <div class="ad-kicker">The computer<br>that starts<br>with you.</div>
    <div class="cols-2 tall">
      <div>
        <p class="lead">The Centauri64 arrives ready to program. No software package to load. No mysterious setup. Switch on, type an instruction and the machine answers.</p>
        ${listing(['PRINT "HELLO"'], { tint: true })}
        <p>That is the beginning. What comes next is up to you.</p>
        <div class="subhead red">Built in</div>
        <p>Centauri BASIC · 32 colours · 64K memory · High Resolution and Arcade display modes.</p>
        <div class="subhead">This month</div>
        <p>Learn BASIC from the blinking cursor to a complete adventure — then alter the cover game yourself.</p>
        <div class="subhead">What you get</div>
        <p>A real home computer that boots straight into BASIC. Type at the prompt and it answers. Number your lines and it remembers. SAVE to tape and LOAD tomorrow. The same Centauri BASIC printed in this magazine is the language in the machine — no special edition, no hidden commands.</p>
        <div class="tip">
          <div class="tip-label">For people who make things</div>
          <p>Games. Utilities. Experiments that crash and get fixed. The Centauri64 is for fingers on keys, not for watching other people's software forever.</p>
        </div>
      </div>
      <div>
        <div class="box filled">
          <div class="price"><span>Starter Pack</span>£199</div>
          <hr class="rule" style="border-color:#ffe45a;margin:2mm 0">
          <p style="margin:0">64K Centauri64<br>Welcome tape<br>Issue 1 magazine<br>1 game — Dungeon of Ghoule</p>
        </div>
        <div class="callout" style="margin-top:2.5mm">
          <div class="tip-label">Centauri Computer Systems</div>
          <p>The home computer for people who want to make things.</p>
        </div>
        <div class="box" style="margin-top:2.5mm">
          <div class="subhead black">Ready when you are</div>
          <p style="margin:0 0 1.2mm">Open the box. Plug in. Switch on. When you see READY. you have already started.</p>
          ${listing(["READY.", 'PRINT "HELLO"', "HELLO"], { tint: false, compact: true })}
        </div>
        <div class="pull" style="margin-top:3mm">From HELLO to Castle Ghoule in one issue.</div>
      </div>
    </div>
  </div>
  ${footer("Advertisement", "2")}
</section>`);

// PAGE 3 — contents
pages.push(`
<section class="page">
  <div class="inner">
    <div class="mast">
      <div class="brand">Centauri64</div>
      <div class="meta">Vol 1 · No 1 · January 1986</div>
    </div>
    <p class="lead">Welcome to the first issue. This month we begin at the BASIC prompt and finish inside Castle Ghoule. Every listing here has been written for the same Centauri BASIC that you use — no secret magazine version.</p>
    <hr class="rule thick red">
    <div class="toc-row"><div class="cat">Getting started</div><div><div class="title">Meet Your Centauri64</div><div class="blurb">Immediate commands, numbered lines, LIST and RUN.</div></div><div class="num">4</div></div>
    <div class="toc-row"><div class="cat">Basic 1</div><div><div class="title">Your First Program</div><div class="blurb">GOTO, NEW, SAVE and LOAD.</div></div><div class="num">5</div></div>
    <div class="toc-row"><div class="cat">Basic 2</div><div><div class="title">Memory, Maths &amp; Input</div><div class="blurb">Variables, whole-number arithmetic, strings and INPUT.</div></div><div class="num">6</div></div>
    <div class="toc-row"><div class="cat">Basic 3</div><div><div class="title">Making Decisions</div><div class="blurb">IF…THEN and the first tiny adventure.</div></div><div class="num">8</div></div>
    <div class="toc-row"><div class="cat">Basic 4</div><div><div class="title">GOTO &amp; Randomness</div><div class="blurb">Build rooms, then let RND surprise the player.</div></div><div class="num">10</div></div>
    <div class="toc-row"><div class="cat">Basic 5</div><div><div class="title">Colour &amp; Reset</div><div class="blurb">INK, PAPER, CLS and getting back to normal.</div></div><div class="num">11</div></div>
    <div class="toc-row"><div class="cat">Type-in</div><div><div class="title">Guess My Number</div><div class="blurb">A complete game using everything learned so far.</div></div><div class="num">12</div></div>
    <div class="toc-row"><div class="cat">Cover tape</div><div><div class="title">Dungeon of Ghoule</div><div class="blurb">Play it, LIST it, understand it, hack it.</div></div><div class="num">14</div></div>
    <div class="toc-row"><div class="cat">Challenge</div><div><div class="title">Your First Program</div><div class="blurb">Send us your software. Payment: £2.</div></div><div class="num">19</div></div>
    <div class="toc-row"><div class="cat">Next month</div><div><div class="title">Graphics!</div><div class="blurb">High Resolution drawing and Lunar Rescue.</div></div><div class="num">20</div></div>
    <hr class="rule thick" style="margin-top:3mm">
    <div class="cols-3">
      <div>
        <div class="subhead red">How to use this issue</div>
        <p>Read from the front if you are new. Jump to page 12 if you want a game tonight. Jump to page 14 if the cover tape is calling.</p>
      </div>
      <div>
        <div class="subhead">Listings</div>
        <p>Type them carefully. LIST often. If something fails, the bug is usually a missing quote or a wrong line number — hunt it down.</p>
      </div>
      <div>
        <div class="subhead">Cover tape</div>
        <p>LOAD DUNGEON from My Software once Issue 1 has granted the tape. Then LIST it. The magazine explains what you will see.</p>
      </div>
    </div>
  </div>
  ${footer("Contents", "3")}
</section>`);

// PAGE 4 — Meet Your Centauri64
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label">Getting started</div>
    <h1 class="headline">Meet Your<br>Centauri64</h1>
    <p class="deck">The blinking cursor is the machine asking what you want to do.</p>
    <hr class="rule">
    <div class="cols-3 tall">
      <div>
        <p>Switch on and BASIC is ready. Simple programs run on the normal screen, directly beneath the editor. You only leave it when a program deliberately changes display mode.</p>
        <p>At the prompt, type <strong>PRINT "HELLO"</strong>. With no line number the instruction happens immediately. HELLO appears, but the instruction is not stored.</p>
        <p>Now add a line number: <strong>10 PRINT "HELLO"</strong>. This time nothing is printed. The Centauri64 has remembered the line as part of a program.</p>
        <div class="anno"><strong>Why the delay?</strong> Numbered lines are building materials. Immediate lines are tools. You need both.</div>
        <p>Type <strong>LIST</strong> to see the stored program. Type <strong>RUN</strong> to execute it. BASIC begins at the lowest numbered line and works down.</p>
        <p>Programmers commonly number lines 10, 20, 30 rather than 1, 2, 3. The gaps leave room to insert extra instructions later.</p>
      </div>
      <div>
        <div class="listing-head">Immediate</div>
        ${listing(['PRINT "HELLO"', "HELLO"], { tint: true })}
        <div class="listing-head">Stored</div>
        ${listing(['10 PRINT "HELLO"', "LIST", '10 PRINT "HELLO"'], { tint: true })}
        <div class="listing-head">Run it</div>
        ${listing(["RUN", "HELLO"], { tint: true })}
        <div class="listing-head">Also try</div>
        ${listing(["PRINT 2+2", "4", "A=10", "PRINT A", "10"], { tint: true, compact: true })}
      </div>
      <div>
        <div class="tip">
          <div class="tip-label">Try it</div>
          <p>PRINT 2+2 works immediately too. Experiment at the prompt; numbered lines are for the program you are building.</p>
        </div>
        <div class="callout">
          <div class="tip-label">Survival guide</div>
          <p>If a running program will not stop by itself, use the normal Centauri64 break control to return to BASIC.</p>
        </div>
        <div class="subhead red">Checklist</div>
        <ul class="dense">
          <li>Can you PRINT a word immediately?</li>
          <li>Can you store a numbered line?</li>
          <li>Does LIST show it?</li>
          <li>Does RUN print it?</li>
        </ul>
        <div class="box">
          <div class="subhead black">Common mistake</div>
          <p style="margin:0">Forgetting the quotes around text: PRINT HELLO looks for a variable named HELLO. PRINT "HELLO" prints the word.</p>
        </div>
        <div class="pull">When READY. returns, you are back in charge.</div>
      </div>
    </div>
    <div class="bottom-band cols-3">
      <div>
        <div class="subhead black">Immediate vs stored</div>
        <p>Immediate statements answer now and vanish. Numbered lines wait for RUN. Use the prompt to experiment; use numbers when you want a program you can LIST.</p>
      </div>
      <div>
        <div class="subhead black">Line numbers</div>
        <p>10, 20, 30 leave room for 15. If you number 1, 2, 3 you will run out of gaps the first time you need an extra PRINT.</p>
      </div>
      <div>
        <div class="subhead black">Next page</div>
        <p>Two lines make a loop with GOTO. Then NEW, SAVE and LOAD turn experiments into tapes you can keep.</p>
      </div>
    </div>
  </div>
  ${footer("Getting started", "4")}
</section>`);

// PAGE 5 — Your First Program
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label">Basic programming 1</div>
    <h1 class="headline sm">Your First<br>Program</h1>
    <p class="deck">Two lines make a loop. Four commands make programming practical.</p>
    <hr class="rule">
    <div class="cols-2-1 tall">
      <div>
        ${listing([
          '10 PRINT "MY CENTAURI64 RULES!"',
          "20 GOTO 10",
        ])}
        <p>RUN it. Line 10 prints the message. Line 20 sends execution back to line 10. The message repeats until you stop the program.</p>
        <div class="subhead red">Change it</div>
        ${listing([
          '10 PRINT "GHOULE IS COMING!"',
          '15 PRINT "RUN AWAY!"',
          "20 GOTO 10",
        ])}
        <p>Typing an existing line number replaces it. A new line number is inserted in the right place.</p>
        <div class="anno"><strong>Line 15</strong> sits between 10 and 20 because you left gaps. That is why magazines number by tens.</div>
        <div class="subhead">Delete a line</div>
        <p>Type the line number alone and press RETURN. The line vanishes from the program. LIST to confirm.</p>
      </div>
      <div>
        <table class="mini-table">
          <tr><td>NEW</td><td>clears the current program. Save first if you want to keep it.</td></tr>
          <tr><td>LIST</td><td>shows the numbered program currently in memory.</td></tr>
          <tr><td>SAVE</td><td>stores your current program on a Centauri64 tape.</td></tr>
          <tr><td>LOAD</td><td>brings a saved tape back so you can RUN or LIST it.</td></tr>
        </table>
        <div class="tip">
          <div class="tip-label">Good habit</div>
          <p>Save a copy before making wild changes. Wild changes are encouraged.</p>
        </div>
        <div class="callout">
          <div class="tip-label">Debugging tip</div>
          <p>If the loop never stops printing, that is success — until you break out. If nothing prints, LIST and check the quotes.</p>
        </div>
        <div class="pull">A loop is the first time the computer feels alive.</div>
        <div class="box">
          <div class="subhead black">Things to try</div>
          <ul class="dense" style="margin:0">
            <li>Change the message on line 10.</li>
            <li>Add line 15 with a second PRINT.</li>
            <li>SAVE the program. NEW. LOAD it back.</li>
          </ul>
        </div>
      </div>
    </div>
  </div>
  ${footer("Basic programming 1", "5")}
</section>`);

// PAGE 6 — Variables
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label">Basic programming 2</div>
    <h1 class="headline sm">Give It a<br>Memory</h1>
    <p class="deck">Variables are simply names attached to values.</p>
    <hr class="rule">
    <div class="cols-3">
      <div>
        <div class="subhead">Store a number</div>
        ${listing(["10 GOLD=10", "20 PRINT GOLD"], { compact: true })}
        <div class="subhead">Change it</div>
        ${listing(["10 GOLD=10", "20 GOLD=GOLD+5", "30 PRINT GOLD"], { compact: true })}
        <div class="subhead">Whole numbers</div>
        ${listing(["10 A=10/3", "20 PRINT A", "3"], { compact: true })}
      </div>
      <div>
        <p>Think of GOLD as a labelled place in memory. GOLD=10 puts the value 10 there. GOLD=GOLD+5 takes the old value, adds five, then stores the answer.</p>
        <p>Centauri BASIC works with whole numbers. Ten divided by three gives 3, not 3.333. For scores, lives, counters and game logic that is often exactly what you want.</p>
        <p>Multiplication and division happen before addition and subtraction. Brackets let you change the order: 2+3*4 gives 14, while (2+3)*4 gives 20.</p>
      </div>
      <div>
        <div class="tip">
          <div class="tip-label">Game thinking</div>
          <p>A number can remember an event as well as a quantity. Dungeon of Ghoule will use 0 for ‘no key’ and 1 for ‘key found’.</p>
        </div>
        <div class="box">
          <div class="subhead black">Why whole numbers?</div>
          <p style="margin:0">Scores stay tidy. IF comparisons stay exact. You are writing games, not spreadsheets.</p>
        </div>
        <p>Try printing GOLD before you set it. Then set it. Then change it. Watch the labelled place update.</p>
      </div>
    </div>
  </div>
  ${footer("Basic programming 2", "6")}
</section>`);

// PAGE 7 — INPUT / strings
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label">Basic programming 2</div>
    <h1 class="headline sm">Talk to Your<br>Computer</h1>
    <p class="deck">INPUT turns a program from a speech into a conversation.</p>
    <hr class="rule">
    <div class="cols-2">
      <div>
        ${listing([
          '10 PRINT "WHAT IS YOUR NAME?"',
          '20 INPUT "NAME";N$',
          '30 PRINT "HELLO "+N$',
        ])}
        <p>A normal variable holds a number. A variable ending in $ holds text. N$ remembers whatever the player types. The + on line 30 joins two pieces of text.</p>
        <div class="subhead red">Begin an adventure</div>
        ${listing([
          "10 CLS",
          '20 PRINT "WELCOME TO CASTLE GHOULE!"',
          '30 INPUT "WHAT IS YOUR NAME";N$',
          '40 PRINT "WELCOME, "+N$',
          '50 PRINT "THE DOOR CREAKS OPEN..."',
        ], { compact: true })}
      </div>
      <div>
        <p>The player's own answer has become part of the story. Next we need to let an answer change what happens.</p>
        <div class="tip">
          <div class="tip-label">Try it</div>
          <p>Change N$ to HERO$. Ask another question and store its answer in a second $ variable.</p>
        </div>
        <div class="callout">
          <div class="tip-label">Remember</div>
          <p>CLS clears the normal BASIC screen. Text programs do not need a separate display mode.</p>
        </div>
        <div class="pull">Next: INPUT gives us a choice. IF…THEN will let the computer decide what that choice means.</div>
      </div>
    </div>
  </div>
  ${footer("Basic programming 2", "7")}
</section>`);

// PAGE 8 — IF/THEN
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label">Basic programming 3</div>
    <h1 class="headline sm">Making<br>Decisions</h1>
    <p class="deck">IF…THEN is the fork in the road.</p>
    <hr class="rule">
    <div class="cols-2-1">
      <div>
        ${listing([
          '10 INPUT "HOW MUCH GOLD";GOLD',
          "20 IF GOLD>10 THEN GOTO 100",
          '30 PRINT "YOU CANNOT AFFORD IT!"',
          "40 END",
          '100 PRINT "SOLD!"',
        ], { compact: true })}
        <p>IF checks a condition. If it is true, THEN tells BASIC what to do. Here, having more than 10 gold sends execution to line 100.</p>
        <div class="subhead">Useful comparisons</div>
        <table class="mini-table">
          <tr><td>=</td><td>equal</td></tr>
          <tr><td>&lt;</td><td>less than</td></tr>
          <tr><td>&gt;</td><td>greater than</td></tr>
          <tr><td>&lt;=</td><td>less or equal</td></tr>
          <tr><td>&gt;=</td><td>greater or equal</td></tr>
          <tr><td>&lt;&gt;</td><td>not equal</td></tr>
        </table>
      </div>
      <div>
        <div class="subhead red">A two-choice game</div>
        ${listing([
          "10 CLS",
          '20 PRINT "YOU STAND BEFORE A DARK DOOR."',
          '40 PRINT "1 OPEN THE DOOR"',
          '50 PRINT "2 RUN AWAY"',
          '60 INPUT "CHOICE";A',
          "70 IF A=1 THEN GOTO 100",
          "80 IF A=2 THEN GOTO 200",
          "90 GOTO 40",
          '100 PRINT "THE DOOR CREAKS OPEN..."',
          "110 END",
          '200 PRINT "YOU RUN HOME!"',
          "210 END",
        ], { compact: true })}
        <p>The player chooses. INPUT remembers the choice. IF decides where the program goes. That is already enough to make a game.</p>
        <p><em>Dungeon of Ghoule</em> uses the same trick. Its castle is bigger, but the machinery underneath is no more mysterious.</p>
      </div>
    </div>
  </div>
  ${footer("Basic programming 3", "8")}
</section>`);

// PAGE 9 — Tiny adventure
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label">Basic programming 3</div>
    <h1 class="headline sm">Build a Tiny<br>Adventure</h1>
    <p class="deck">Use groups of line numbers like rooms on a map.</p>
    <hr class="rule">
    <div class="cols-1-2">
      <div>
        <p>BASIC normally runs down through the numbered lines. GOTO changes the route. Keeping locations in separate ranges makes a longer listing easier to understand.</p>
        <div class="tip">
          <div class="tip-label">Add a room!</div>
          <p>Add option 3 to the Hall and send it to line 400. Dungeon? Treasure room? Dragon's lair? You decide.</p>
        </div>
        <div class="callout">
          <div class="tip-label">Why the gaps?</div>
          <p>Using 200, 300, 400 leaves space for extra lines inside each room later.</p>
        </div>
        <div class="pull">Hall → Kitchen → Cellar. Same pattern as Castle Ghoule.</div>
      </div>
      <div>
        ${listing([
          "10 CLS",
          '20 PRINT "YOU ARE IN A DARK HALL."',
          '40 PRINT "1 GO TO THE KITCHEN"',
          '50 PRINT "2 GO TO THE CELLAR"',
          '60 INPUT "CHOICE";A',
          "70 IF A=1 THEN GOTO 200",
          "80 IF A=2 THEN GOTO 300",
          "90 GOTO 40",
          "200 CLS",
          '210 PRINT "THE KITCHEN"',
          '230 PRINT "A CAKE SITS ON THE TABLE."',
          '250 PRINT "1 RETURN TO THE HALL"',
          '260 INPUT "CHOICE";A',
          "270 IF A=1 THEN GOTO 10",
          "300 CLS",
          '310 PRINT "THE CELLAR"',
          '330 PRINT "IT IS COLD AND DARK."',
          '350 PRINT "1 RETURN TO THE HALL"',
          '360 INPUT "CHOICE";A',
          "370 IF A=1 THEN GOTO 10",
        ], { compact: true })}
      </div>
    </div>
  </div>
  ${footer("Basic programming 3", "9")}
</section>`);

// PAGE 10 — RND
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label">Basic programming 4</div>
    <h1 class="headline sm">Leave It<br>to Chance</h1>
    <p class="deck">RND lets the Centauri64 surprise the player.</p>
    <hr class="rule">
    <div class="cols-3">
      <div>
        <div class="subhead">What does RND do?</div>
        ${listing(["10 PRINT RND(2)", "20 GOTO 10"], { compact: true })}
        <div class="box">
          <div class="subhead black">0 or 1</div>
          <p style="margin:0">RND(2) produces 0 or 1. RND(10) produces a whole number from 0 to 9.</p>
        </div>
      </div>
      <div>
        <div class="subhead">An event</div>
        ${listing([
          "20 R=RND(2)",
          "30 IF R=0 THEN GOTO 100",
          "40 GOTO 200",
          '100 PRINT "YOU FOUND A GOLD COIN!"',
          "110 END",
          '200 PRINT "A BAT FLIES PAST!"',
          "210 END",
        ], { compact: true })}
        <p>Run the program several times. The same listing can produce different events because the computer makes one of the choices.</p>
      </div>
      <div>
        <div class="subhead red">From the Crypt</div>
        ${listing([
          "696 R=RND(2)",
          '697 PRINT ""',
          "698 IF R=0 THEN GOTO 1200",
          "699 GOTO 1210",
        ], { compact: true })}
        <p>Those four lines come straight from <em>Dungeon of Ghoule</em>.</p>
        <div class="tip">
          <div class="tip-label">Things to try</div>
          <p>Change RND(2) to RND(3). Add a third outcome. Surprise yourself.</p>
        </div>
      </div>
    </div>
  </div>
  ${footer("Basic programming 4", "10")}
</section>`);

// PAGE 11 — Colour
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label">Basic programming 5</div>
    <h1 class="headline sm">Colour Your<br>Centauri</h1>
    <p class="deck">INK chooses the writing. PAPER chooses the page.</p>
    <hr class="rule">
    <div class="cols-2">
      <div>
        ${listing([
          "10 PAPER 0",
          "20 INK 15",
          "30 CLS",
          '40 PRINT "WELCOME TO MY PROGRAM!"',
        ])}
        <p>Set PAPER before CLS if you want the display cleared to the new background colour. INK changes the colour used by text that follows.</p>
        <div class="subhead">32 colours</div>
        <div class="swatches">
          ${[0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31]
            .map((n) => {
              const rgb = palette(n);
              const ink = n === 1 || n === 7 || n === 15 || n === 31 ? "#111" : "#fff";
              return `<div class="swatch" style="background:${rgb};color:${ink}">${n}</div>`;
            })
            .join("")}
        </div>
      </div>
      <div>
        <div class="callout">
          <div class="tip-label">Help! What have I done?</div>
          <p>Experimenting with colours is half the fun. If the BASIC screen ends up in settings you no longer want, type <strong>RESET</strong> at the prompt.</p>
        </div>
        <div class="box filled" style="text-align:center;padding:4mm">
          <div style="font-family:Impact,sans-serif;font-size:28pt;letter-spacing:2px">RESET</div>
        </div>
        <div class="tip">
          <div class="tip-label">New is different</div>
          <p>NEW clears the program in memory. RESET restores machine settings. They solve two different problems.</p>
        </div>
        <p>Paper 0 and Ink 15 give a classic dark screen with light text — the mood used by <em>Dungeon of Ghoule</em>.</p>
      </div>
    </div>
    <div class="bottom-band cols-3">
      <div>
        <div class="subhead black">Order matters</div>
        <p>PAPER then INK then CLS. If you CLS first, you clear to the old paper colour and wonder why nothing changed.</p>
      </div>
      <div>
        <div class="subhead black">Try a title screen</div>
        ${listing(["10 PAPER 16", "20 INK 7", "30 CLS", '40 PRINT "CASTLE GHOULE"'], { compact: true })}
      </div>
      <div>
        <div class="subhead black">Ready for a game</div>
        <p>You now have words, memory, choices, chance and colour. Page 12 puts them all in one type-in.</p>
      </div>
    </div>
  </div>
  ${footer("Basic programming 5", "11")}
</section>`);

function palette(i) {
  // Approximate Centauri64 palette for print swatches (from CentauriPalette)
  const colors = [
    "#000000","#ffffff","#880000","#aaffee","#cc44cc","#00cc55","#0000aa","#eeee77",
    "#dd8855","#664400","#ff7777","#333333","#777777","#aaff66","#0088ff","#bbbbbb",
    "#141e37","#23375a","#286478","#46b4aa","#195a37","#6ea046","#5a3723","#8c5a28",
    "#c8a050","#3c2850","#784690","#c85a8c","#f0c8a0","#dcd2b4","#a0a0b4","#e8e0d0",
  ];
  return colors[i] || "#888";
}

// PAGE 12 — Guess My Number
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label orange">Type-in game</div>
    <h1 class="guess-head">Guess My<br>Number</h1>
    <p class="deck">Your first complete type-in game — using nothing you haven't already learned.</p>
    <hr class="rule blue">
    <div class="cols-1-2 tall">
      <div>
        <p>Type the listing carefully, then RUN it. The Centauri64 chooses a secret number from 0 to 9 and gives you higher/lower clues until you find it.</p>
        <div class="subhead">What's happening?</div>
        <div class="anno"><strong>10–30</strong> Mood: dark paper, light ink, clear screen.</div>
        <div class="anno"><strong>40</strong> SECRET=RND(10) picks 0…9.</div>
        <div class="anno"><strong>100</strong> INPUT waits for your guess in A.</div>
        <div class="anno"><strong>110–120</strong> Compare. Equal? Win. Lower? Jump to “too low”.</div>
        <div class="anno"><strong>140 / 170</strong> Back to the INPUT — the guessing loop.</div>
        <div class="anno"><strong>210</strong> INK 7 makes the win message stand out.</div>
        <div class="tip">
          <div class="tip-label">Type it — don't paste it</div>
          <p>The point of a type-in is that your fingers and brain meet every line. If you make an error, LIST the program and hunt it down.</p>
        </div>
        <div class="callout">
          <div class="tip-label">If it misbehaves</div>
          <p>Wrong comparisons often mean a typing error on line 110 or 120. LIST those lines alone and re-enter them.</p>
        </div>
      </div>
      <div>
        ${listing([
          "10 PAPER 0",
          "20 INK 15",
          "30 CLS",
          "40 SECRET=RND(10)",
          '50 PRINT "GUESS MY NUMBER!"',
          '60 PRINT ""',
          '70 PRINT "I AM THINKING OF A NUMBER"',
          '80 PRINT "FROM 0 TO 9."',
          '90 PRINT ""',
          '100 INPUT "YOUR GUESS";A',
          "110 IF A=SECRET THEN GOTO 200",
          "120 IF A<SECRET THEN GOTO 160",
          '130 PRINT "TOO HIGH!"',
          "140 GOTO 100",
          '160 PRINT "TOO LOW!"',
          "170 GOTO 100",
          '200 PRINT ""',
          "210 INK 7",
          '220 PRINT "YOU GOT IT!"',
          "230 END",
        ], { compact: true, tint: true })}
        <div class="pull">Every command on this page appeared earlier in the issue.</div>
      </div>
    </div>
    <div class="bottom-band cols-3">
      <div>
        <div class="subhead black">Sample run</div>
        ${listing(["YOUR GUESS? 3", "TOO LOW!", "YOUR GUESS? 8", "TOO HIGH!", "YOUR GUESS? 5", "YOU GOT IT!"], { compact: true })}
      </div>
      <div>
        <div class="subhead black">Checksum habit</div>
        <p>After typing, LIST. Count the lines. You should see 10 through 230 with no gaps where you meant to type. Missing 160 is a classic way to break the “too low” branch.</p>
      </div>
      <div>
        <div class="subhead black">Then own it</div>
        <p>Page 13 is not optional homework. Change the range, the messages, the colours. The type-in becomes your game the moment you stop copying.</p>
      </div>
    </div>
  </div>
  ${footer("Type-in game", "12")}
</section>`);

// PAGE 13 — Make it yours
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label orange">Type-in game</div>
    <h1 class="headline sm">Now Make<br>It Yours</h1>
    <p class="deck">The best type-in is the one you stop copying.</p>
    <hr class="rule">
    <div class="cols-3 tall">
      <div>
        <div class="subhead red">Make it harder</div>
        ${listing(["40 SECRET=RND(100)", '80 PRINT "FROM 0 TO 99."'], { compact: true })}
        <p>Two changes make the game much tougher.</p>
        <div class="subhead">Make it meaner</div>
        <p>Remove the TOO HIGH and TOO LOW clues. Now the player only learns whether the guess is correct.</p>
        <div class="subhead">Add lives</div>
        ${listing(["35 LIVES=5", "105 LIVES=LIVES-1", "106 IF LIVES=0 THEN GOTO 300"], { compact: true })}
        <p>Then print a GAME OVER message at line 300. You invent the exact wording.</p>
      </div>
      <div>
        <div class="subhead">Add a score</div>
        <p>Can you count how many guesses the player makes? Start with GUESSES=0 near the top. Add one after each INPUT. PRINT the total when they win.</p>
        ${listing(["35 GUESSES=0", "102 GUESSES=GUESSES+1", '225 PRINT "GUESSES:";GUESSES'], { compact: true })}
        <p>A type-in is not a museum piece. Change the title. Change the range. Change the colours. Add messages. Break it, LIST it, repair it.</p>
      </div>
      <div>
        <div class="tip">
          <div class="tip-label">Challenge</div>
          <p>Can you make the computer choose from 1 to 10 instead of 0 to 9? Think about what RND(10) actually returns.</p>
        </div>
        <div class="callout">
          <div class="tip-label">Save it</div>
          <p>Give your version a new tape name. You have made a real Centauri64 game.</p>
        </div>
        <div class="pull">You are no longer following a listing. You are writing software.</div>
        <div class="box">
          <div class="subhead black">Then open the castle</div>
          <p style="margin:0">Page 14 begins Dungeon of Ghoule — the same tools, a much larger story.</p>
        </div>
      </div>
    </div>
  </div>
  ${footer("Type-in game", "13")}
</section>`);

// PAGE 14 — Dungeon intro
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label red">Cover tape</div>
    <h1 class="dungeon-head"><span class="small">Dare you enter Castle Ghoule?</span>Dungeon of<br>Ghoule</h1>
    <p class="dungeon-deck">High above the village stands Castle Ghoule. Nobody has entered its gates in years — or at least, nobody who has returned.</p>
    <hr class="rule red">
    <div class="cols-2 tall">
      <div>
        <p>Tonight the gates are open. Explore the ruined castle, search its forgotten rooms and discover the secret guarded by the ghost in the tower. Choose carefully. Not everything buried beneath Castle Ghoule wants to be found.</p>
        <p><strong>How to play:</strong> type the number beside your choice and press RETURN. Your decisions decide where the story goes.</p>
        <p>The most important thing about this month's cover game is not the treasure. It is that you can LOAD it, LIST it and see every instruction that makes the adventure work.</p>
        <div class="subhead red">Opening excerpt</div>
        ${listing([
          "30 PAPER 0",
          "40 INK 15",
          "50 CLS",
          "60 KEY=0",
          '70 PRINT "DUNGEON OF GHOULE"',
          '150 PRINT "1 ENTER THE CASTLE"',
          '160 PRINT "2 RUN AWAY"',
          '170 INPUT "CHOICE";A',
        ], { compact: true })}
        <p>Mood first. Memory second. Choices third. That is the opening of almost every text adventure worth typing.</p>
      </div>
      <div>
        <div class="band">On this month's cover tape</div>
        <div class="box filled" style="text-align:center;padding:4mm 3mm">
          <div style="font-family:'Old English Text MT',Georgia,serif;font-size:20pt;color:#ffe45a;line-height:1">Dungeon of Ghoule</div>
          <div style="font-family:Arial,sans-serif;font-size:8pt;letter-spacing:1.5px;margin-top:2mm;text-transform:uppercase">Load it · Run it · List it · Change it</div>
        </div>
        <div class="tip">
          <div class="tip-label">Before you hack</div>
          <p>SAVE your own copy first. Then the castle is yours.</p>
        </div>
        <div class="pull">You already know how this game works.</div>
        <div class="box">
          <div class="subhead black">What you will recognise</div>
          <ul class="dense" style="margin:0">
            <li>PRINT for rooms and choices</li>
            <li>INPUT for decisions</li>
            <li>IF…THEN / GOTO for routes</li>
            <li>KEY for memory</li>
            <li>RND in the Crypt</li>
            <li>INK / PAPER / CLS for atmosphere</li>
          </ul>
        </div>
        <div class="callout">
          <div class="tip-label">Over the next pages</div>
          <p>We open the listing. Not every line — the parts that teach. Then we dare you to change them.</p>
        </div>
      </div>
    </div>
  </div>
  ${footer("Cover tape", "14")}
</section>`);

// PAGE 15 — How it works
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label red">Cover tape</div>
    <h1 class="headline sm">How Does<br>It Work?</h1>
    <p class="deck">There is no secret adventure command. You already know the machinery.</p>
    <hr class="rule">
    <div class="cols-2">
      <div>
        ${listing([
          '150 PRINT "1 ENTER THE CASTLE"',
          '160 PRINT "2 RUN AWAY"',
          '170 INPUT "CHOICE";A',
          "180 IF A=1 THEN GOTO 200",
          "190 IF A=2 THEN GOTO 900",
        ], { compact: true })}
        <p><em>Dungeon of Ghoule</em> prints choices, waits for a number, then uses IF and GOTO to decide where execution continues.</p>
        <div class="subhead red">The castle in line numbers</div>
        <table class="mini-table">
          <tr><td>200</td><td>Great Hall</td></tr>
          <tr><td>400</td><td>Tower</td></tr>
          <tr><td>500</td><td>Cellar</td></tr>
          <tr><td>600</td><td>Crypt</td></tr>
          <tr><td>700</td><td>Ghost</td></tr>
          <tr><td>1000</td><td>Treasure Room</td></tr>
        </table>
      </div>
      <div>
        <p>Keeping locations in separate groups of line numbers makes a long listing easier to understand. It also leaves room to insert new instructions.</p>
        <p>Notice how the Great Hall acts as a hub. Several choices send the player out to other locations; those locations can send the player back.</p>
        <p>Once you see the listing as a collection of small sections rather than one enormous program, the adventure becomes much less intimidating.</p>
        <div class="callout">
          <div class="tip-label">The Great Hall</div>
          <p>Stairs, cellar, crypt, leave — four doors from one room. That hub pattern is half of adventure design.</p>
        </div>
      </div>
    </div>
  </div>
  ${footer("Cover tape", "15")}
</section>`);

// PAGE 16 — Silver key
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label red">Cover tape</div>
    <h1 class="headline sm">The Silver<br>Key</h1>
    <p class="deck">One variable gives the adventure a memory.</p>
    <hr class="rule">
    <div class="cols-3">
      <div>
        <div class="subhead">At the start</div>
        ${listing(["60 KEY=0"], { compact: true })}
        <div class="subhead">In the cellar</div>
        ${listing(["570 KEY=1", '575 PRINT "YOU FOUND A SILVER KEY!"'], { compact: true })}
        <div class="subhead">At the ghost</div>
        ${listing(["730 IF KEY=1 THEN GOTO 800"], { compact: true })}
      </div>
      <div>
        <p>That's the whole trick. KEY=0 means the player has not found the key. KEY=1 means they have. When the player meets the ghost, the program checks the value.</p>
        <p>A variable does not need to represent a visible number. It can remember whether something happened earlier. That gives the game a history.</p>
        <p>The same idea can represent a door switch, a rescued prisoner, a treasure already collected, a boss defeated or a conversation already heard.</p>
      </div>
      <div>
        <div class="tip">
          <div class="tip-label">The lesson</div>
          <p>You do not need a special inventory command to begin making adventures. A handful of well-chosen variables can remember a surprising amount about the player's journey.</p>
        </div>
        <div class="pull">KEY is not a score. KEY is a story.</div>
        <div class="box">
          <div class="subhead black">Trace it</div>
          <p style="margin:0">LOAD DUNGEON. LIST. Find lines 60, 570 and 730. Watch the same idea in three places.</p>
        </div>
      </div>
    </div>
  </div>
  ${footer("Cover tape", "16")}
</section>`);

// PAGE 17 — RND + summary
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label red">Cover tape</div>
    <h1 class="headline sm">Expect the<br>Unexpected</h1>
    <p class="deck">The Crypt does not behave the same way every time.</p>
    <hr class="rule">
    <div class="cols-2">
      <div>
        ${listing([
          "696 R=RND(2)",
          '697 PRINT ""',
          "698 IF R=0 THEN GOTO 1200",
          "699 GOTO 1210",
        ])}
        <p>One result sends bats bursting from the walls. The other gives the player a whispered clue about the silver key.</p>
        <div class="band black">Dungeon of Ghoule is really just…</div>
        <table class="mini-table">
          <tr><td>PRINT</td><td>describe the world</td></tr>
          <tr><td>INPUT</td><td>ask what the player wants</td></tr>
          <tr><td>IF</td><td>make decisions</td></tr>
          <tr><td>GOTO</td><td>move around the adventure</td></tr>
          <tr><td>VARIABLES</td><td>remember what happened</td></tr>
          <tr><td>RND</td><td>add surprise</td></tr>
          <tr><td>INK/PAPER/CLS</td><td>set the mood</td></tr>
        </table>
      </div>
      <div>
        <div class="callout">
          <div class="tip-label">No secret engine</div>
          <p>The cover game is made from the same BASIC you've been learning. LOAD it, LIST it, read it and change it.</p>
        </div>
        <div class="box filled">
          <div class="subhead">You already know</div>
          <p style="margin:0">PRINT · INPUT · variables · strings · IF · GOTO · RND · INK · PAPER · CLS</p>
          <hr class="rule" style="border-color:#ffe45a;margin:2mm 0">
          <p style="margin:0">Put together, they feel much larger than any one command.</p>
        </div>
        <div class="pull">A castle is only rooms, choices and memory.</div>
      </div>
    </div>
  </div>
  ${footer("Cover tape", "17")}
</section>`);

// PAGE 18 — Hack the dungeon
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label magenta">Hack it!</div>
    <h1 class="headline sm">Hack the<br>Dungeon!</h1>
    <p class="deck">Don't just play the cover tape. Make it yours.</p>
    <hr class="rule">
    <div class="cols-2 tall">
      <div>
        <p><span class="challenge-num">1</span><strong>EASY — Rewrite the ghost</strong><br>Change the ghost's dialogue. Make it frightening, friendly or ridiculous.</p>
        <p style="clear:both"><span class="challenge-num">2</span><strong>EASY — Change the treasure</strong><br>Gold is predictable. What else could be hidden in the secret room?</p>
        <p style="clear:both"><span class="challenge-num">3</span><strong>MEDIUM — Change the Crypt</strong><br>Replace the bats or whispered clue with your own random event.</p>
        <div class="subhead red" style="clear:both;margin-top:3mm">Where to look</div>
        <table class="mini-table">
          <tr><td>700+</td><td>Ghost dialogue and key check</td></tr>
          <tr><td>1000+</td><td>Treasure room text</td></tr>
          <tr><td>696–699</td><td>Crypt randomness</td></tr>
          <tr><td>280–360</td><td>Great Hall choices</td></tr>
        </table>
        <div class="anno"><strong>Method</strong> LIST a section. Change one PRINT. RUN. If it breaks, re-enter the line.</div>
      </div>
      <div>
        <p><span class="challenge-num">4</span><strong>MEDIUM — Add another choice</strong><br>Give one existing room a third option and send it somewhere new.</p>
        <p style="clear:both"><span class="challenge-num">5</span><strong>HARD — Build a new room</strong><br>Create a new location around line 1300 and connect it to Castle Ghoule.</p>
        <div class="callout" style="clear:both;margin-top:2mm">
          <div class="tip-label">Save your version</div>
          <p>Give your altered game a new name and save it to your own tape. Keep the original cover tape untouched so you can always start again.</p>
        </div>
        <div class="tip">
          <div class="tip-label">Starter edit</div>
          <p>Find the Great Hall title PRINT and change it to THE HAUNTED HALL. Instant ownership.</p>
        </div>
        <div class="box">
          <div class="subhead black">New room pattern</div>
          ${listing([
            "1300 CLS",
            '1310 PRINT "THE SECRET LIBRARY"',
            '1320 PRINT "1 RETURN TO THE HALL"',
            '1330 INPUT "CHOICE";A',
            "1340 IF A=1 THEN GOTO 200",
          ], { compact: true, tint: false })}
          <p style="margin:0">Then add a Hall option that GOTOs 1300.</p>
        </div>
      </div>
    </div>
    <div class="bottom-band cols-3">
      <div>
        <div class="subhead black">Keep the original</div>
        <p>LOAD the cover tape, SAVE under a new name immediately, then hack the copy. If you ruin the castle, the original tape is still there.</p>
      </div>
      <div>
        <div class="subhead black">One change at a time</div>
        <p>Change one PRINT. RUN. Change one IF. RUN. Magazines that invite you to rewrite half the listing at once are how type-ins die.</p>
      </div>
      <div>
        <div class="subhead black">Show someone</div>
        <p>When your version feels yours, SAVE it and try the Reader Challenge on page 19 — even a tiny HELLO loop qualifies.</p>
      </div>
    </div>
  </div>
  ${footer("Hack it!", "18")}
</section>`);

// PAGE 19 — Reader challenge
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label green">Reader challenge</div>
    <h1 class="headline sm">Your First<br>Program</h1>
    <div class="band">We want your software!</div>
    <div class="cols-2-1 tall">
      <div>
        <p>Have you written something for your Centauri64? Send it in. It does not need graphics. It does not need sound. It does not even need to be a game. We are looking for something that proves you have started programming your new computer.</p>
        <p>Save it to a tape, give it a description and author, then submit it through the Reader Challenge. Even a tiny looping HELLO program can qualify. This is your first step, not your final exam.</p>
        <div class="subhead">To enter</div>
        <ol style="margin:0 0 2mm;padding-left:4.5mm;font-size:9pt">
          <li>Write a Centauri BASIC program.</li>
          <li>Save it to a tape.</li>
          <li>Add description and author.</li>
          <li>Submit it to the challenge.</li>
        </ol>
        <div class="subhead red">Ideas if you are stuck</div>
        <ul class="dense">
          <li>A looping HELLO with your name in it</li>
          <li>A two-room adventure using IF and GOTO</li>
          <li>A coloured title screen with PAPER / INK / CLS</li>
          <li>Your own version of Guess My Number</li>
        </ul>
        <div class="anno"><strong>Remember</strong> NEW wipes the program. SAVE before you tidy up.</div>
      </div>
      <div>
        <div class="box filled" style="text-align:center;padding:5mm 3mm">
          <div style="font-family:Arial,sans-serif;font-size:8pt;letter-spacing:2px;text-transform:uppercase;color:#ffe45a">Payment</div>
          <div class="price" style="color:#ffe45a">£2</div>
        </div>
        ${listing(['10 PRINT "HELLO"', "20 GOTO 10"], { tint: true })}
        <div class="tip">
          <div class="tip-label">Yes, simple counts</div>
          <p>The point is to get started, finish something and send it. There will be harder challenges later.</p>
        </div>
        <div class="callout">
          <div class="tip-label">What we look for</div>
          <p>That it RUNs. That you wrote it. That you cared enough to SAVE and submit. Style can wait.</p>
        </div>
        <div class="pull">Finish something. Send it. Get paid £2.</div>
      </div>
    </div>
  </div>
  ${footer("Reader challenge", "19")}
</section>`);

// PAGE 20 — Next month
pages.push(`
<section class="page">
  <div class="inner">
    <div class="section-label">Next month · February 1986</div>
    <h1 class="headline lg" style="color:#000078">Graphics!</h1>
    <p class="deck">You've made it talk. Now make it draw.</p>
    <hr class="rule blue">
    <div class="cols-2 tall">
      <div>
        <p>Issue 2 opens the Centauri64's High Resolution display. Plot points. Draw lines. Build rectangles and circles. Add colour. Then combine those ideas into your first graphical game.</p>
        <div class="subhead">You'll learn</div>
        <p>MODE 1 · PLOT · LINE · RECT · CIRCLE · movement · timing · simple graphical game loops</p>
        ${listing(["10 MODE 1", "20 PLOT 160,120", "30 LINE 20,20,300,200"], { tint: true })}
        <div class="subhead red">Why wait?</div>
        <p>Because Issue 1 had to teach the language first. A line on screen is only interesting once you can PRINT, decide, remember and loop. Those habits survive when the pixels arrive.</p>
        <div class="tip">
          <div class="tip-label">Until then</div>
          <p>Finish Guess My Number. LIST Dungeon of Ghoule. Send your Reader Challenge program. February will still be waiting.</p>
        </div>
      </div>
      <div>
        <div class="band blue">On the cover tape</div>
        <div class="box filled" style="text-align:center;padding:6mm 3mm">
          <div style="font-family:Impact,sans-serif;font-size:26pt;line-height:0.95;color:#ffe45a;text-transform:uppercase">Lunar<br>Rescue</div>
          <div style="font-family:Arial,sans-serif;font-size:8pt;letter-spacing:1.2px;margin-top:3mm;text-transform:uppercase">Your first graphical Centauri64 game</div>
        </div>
        <div class="callout">
          <div class="tip-label">On sale next month</div>
          <p>Keep Issue 1 beside the computer. Everything you learned here still matters when the pixels start flying.</p>
        </div>
        <div class="box">
          <div class="subhead black">Issue 1 → Issue 2</div>
          <table class="mini-table">
            <tr><td>Words</td><td>Pixels</td></tr>
            <tr><td>INPUT</td><td>KEYS / timing</td></tr>
            <tr><td>IF / GOTO</td><td>Game loops</td></tr>
            <tr><td>RND</td><td>Hazard &amp; luck</td></tr>
          </table>
        </div>
        <div class="pull">You've made it talk. Now make it draw.</div>
      </div>
    </div>
  </div>
  ${footer("Next month", "20")}
</section>`);

const html = `<!DOCTYPE html>
<html lang="en-GB">
<head>
<meta charset="utf-8">
<title>Centauri64 Magazine — Issue 1 — January 1986 (v4)</title>
<style>${css}</style>
</head>
<body>
${pages.join("\n")}
</body>
</html>`;

fs.writeFileSync(path.join(__dirname, "issue01.html"), html);
console.log(`Wrote issue01.html (${pages.length} pages)`);
