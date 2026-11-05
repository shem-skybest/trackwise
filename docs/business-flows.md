# Business Flows

How work moves through TrackWise from start to finish, one chart per business line. Each chart is
[Mermaid](https://mermaid.js.org/) code: GitHub renders it in place, and you can paste any block
into [mermaid.live](https://mermaid.live) to view, edit, or export it as PNG/SVG. The rendered images
are in [images/flows/](images/flows/).

Colours, in every chart:

- **Green** - built and in use
- **Amber** - built, but with a known gap (noted on the box)
- **Red, dashed** - not built yet
- **Grey, dashed** - happens outside TrackWise (SAS, the driver's Messenger report)

Keep these charts in step with the code. When a status, rule or handoff changes, change the chart in
the same commit. Statuses and rules come from the domain entities named under each chart.

1. [Overview](#1-overview) - setup, the three business lines, and where each one ends
2. [Cold Chain](#2-cold-chain) - reefer truck trips, billed in batches per client period
3. [Freight forwarding](#3-freight-forwarding) - quotation, booking, workfile
4. [Trucking](#4-trucking) - loads, trips, the driver app, invoices and payments

## 1. Overview

```mermaid
flowchart TD
    classDef built fill:#dcfce7,stroke:#16a34a,color:#14532d
    classDef partial fill:#fef3c7,stroke:#d97706,color:#78350f
    classDef missing fill:#fee2e2,stroke:#dc2626,color:#7f1d1d,stroke-dasharray:5 5
    classDef outside fill:#f3f4f6,stroke:#9ca3af,color:#374151,stroke-dasharray:5 5

    subgraph SETUP["1 - Setup: once, then kept up to date"]
        direction LR
        S1["Company, branches,<br/>users and roles"] --> S2["Master data<br/>partners, ports, vessels,<br/>commodities, currencies"]
        S2 --> S3["Fleet and crew<br/>trucks, trailers, gensets,<br/>drivers, helpers"]
        S3 --> S4["Customers and<br/>delivery locations"]
        S4 --> S5["Pricing<br/>tariffs + charge codes,<br/>rate agreements, diesel prices"]
    end

    SETUP --> CC1
    SETUP --> FF1
    SETUP --> TR1

    subgraph CC["2a - Cold Chain: reefer truck trips"]
        CC1["Booking<br/>BKN-RFV number"] --> CC2["Crew plan<br/>+ trip"]
        CC2 --> CC3["Crew report<br/>times, outcomes, photos"]
        CC3 --> CC4["Trip priced<br/>from the rate agreement"]
        CC4 --> CC5["Billing batch<br/>SLC-RFV, one per client period"]
    end

    subgraph FF["2b - Freight forwarding"]
        FF1["Quotation"] --> FF2["Booking"]
        FF2 --> FF3["Workfile<br/>domestic, import, export,<br/>triangular, reefer, trucking"]
        FF3 --> FF4["Manager review"]
        FF4 --> FF5["Accounting review<br/>then Closed"]
        FF6["Workfile charges<br/>and billing"]
        FF5 -.-> FF6
    end

    subgraph TR["2c - Trucking: loads and trips"]
        TR1["Load"] --> TR2["Trip<br/>dispatched"]
        TR2 --> TR3["Driver app<br/>stops + proof of delivery"]
        TR3 --> TR4["Load invoice<br/>sent to customer"]
        TR4 --> TR5["Payment<br/>Stripe or recorded"]
    end

    subgraph COSTS["Running costs, beside every line"]
        direction LR
        X1["Cash funds<br/>tolls, parking, passways"] ~~~ X2["Fuel log"] ~~~ X3["Repairs + PMS"]
    end

    CC5 --> ACC["Accounting bills in SAS<br/>invoice number typed back"]
    FF5 --> ACC
    TR5 --> QBO["QuickBooks sync<br/>if connected"]
    X1 -.->|"tolls billed at cost"| CC5

    class S1,S2,S3,S4,S5,CC1,CC2,CC3,CC4,CC5,FF1,FF2,FF3,FF4,FF5,TR1,TR2,TR3,TR4,TR5,X1,X2,X3,QBO built
    class FF6 missing
    class ACC outside
```

## 2. Cold Chain

One booking is one truck-trip for one client on one day. Cold Chain bookings never get a workfile:
their trips are billed together, one batch per client billing period.

This chart uses Mermaid's ELK layout (the `config` header) so it reads top to bottom. mermaid.live
draws it that way; a viewer without ELK falls back to the default layout, which still works but
spreads diagonally. [2-cold-chain.png](images/flows/2-cold-chain.png) is the clean copy.

```mermaid
---
config:
  layout: elk
---
flowchart TD
    classDef built fill:#dcfce7,stroke:#16a34a,color:#14532d
    classDef partial fill:#fef3c7,stroke:#d97706,color:#78350f
    classDef missing fill:#fee2e2,stroke:#dc2626,color:#7f1d1d,stroke-dasharray:5 5
    classDef outside fill:#f3f4f6,stroke:#9ca3af,color:#374151,stroke-dasharray:5 5

    subgraph BOOK["Booking"]
        RA["Client's rate agreement<br/>Draft → Active<br/>a price change is a new revision"]
        DP["Diesel price<br/>of the service date"]
        BK["Office creates the booking<br/>date, truck size, pickup warehouse,<br/>drops, commodity, temperature<br/>BKN-RFV number + live price estimate"]
        SO["Standing order<br/>copy a booking onto chosen days"]
        RA --> BK
        DP --> BK
        BK -.-> SO
        SO -.->|"one new booking per day"| BK
        BK --> PEN["Pending"]
        PEN --> Q1{"Client<br/>confirms?"}
        Q1 -->|"not yet"| HOLD["Hold / Resched"]
        HOLD --> Q1
        Q1 -->|"yes"| CON["Confirmed"]
        Q1 -->|"no"| LOST["Cancelled, Lost or No booking<br/>loss reason required"]
        CON -->|"called off"| LOST
    end

    subgraph DISPATCH["Dispatch"]
        BB["Booking board<br/>Crew Plan > Bookings tab<br/>confirmed bookings with no trip yet"]
        CP["Crew plan for the day<br/>per truck: status, driver, up to 2 helpers<br/>absent / on-leave crew can't be picked"]
        TRIP["Create trip from booking<br/>one load per drop, crew from the crew plan<br/>Booking: In execution"]
        PR["Trip priced at once<br/>costliest drop's rate + fuel surcharge<br/>+ extra-drop fees<br/>foul-trip % if every drop failed"]
        TDS["Trip: Draft → Dispatched"]
        TCAN["Trip cancelled<br/>booking back to Confirmed"]
        CON --> BB
        BB --> TRIP
        CP --> TRIP
        TRIP --> PR
        PR --> TDS
        TDS -.->|"before it completes"| TCAN
    end

    subgraph RUN["The run"]
        MSG["Driver reports in Messenger<br/>times from GreatJ GPS"]
        CR["Office types the crew report<br/>truck / crew / drops editable<br/>until the first arrival"]
        WH["Warehouse<br/>arrival, loading, departure<br/>+ inspection photos<br/>Trip: In transit"]
        DROP["Each drop<br/>arrival, departure<br/>+ proof-of-delivery photos"]
        OUT{"Drop<br/>outcome"}
        GAR["Garage times"]
        DONE["Trip: Completed<br/>every drop departed with an outcome<br/>Booking: Completed"]
        COR["Correction<br/>reason required, kept as history"]
        OOD["Price out of date<br/>after a corrected outcome, a drop added<br/>or removed, or a different-size truck<br/>re-price on the trip"]
        TDS --> CR
        MSG -.-> CR
        CR --> WH
        WH --> DROP
        DROP --> OUT
        OUT -->|"Delivered"| GAR
        OUT -->|"Foul trip"| GAR
        OUT -->|"Cancelled by client"| GAR
        GAR --> DONE
        CR -.->|"a recorded time or<br/>outcome was wrong"| COR
        COR -.-> OOD
    end

    subgraph BILL["Billing"]
        BD["Billing batch: Draft<br/>one client, one period<br/>1-15 / 16-end, weekly or monthly<br/>trips follow their latest price"]
        CHG["Other charges<br/>demurrage, backload, different location,<br/>tolls + parking from cash funds"]
        PAP["Client papers checklist<br/>before locking"]
        LCK["Locked<br/>refused while any trip is unfinished,<br/>unpriced or out of date<br/>can unlock back to Draft, with reason"]
        FWD["Forwarded<br/>PDF summary printed for Accounting<br/>SLC-RFV number<br/>can unlock back to Draft, with reason"]
        XLS["nSFS Excel transmittal"]
        SAS["Accounting bills the client in SAS"]
        BLD["Billed<br/>SAS invoice number typed in"]
        CAN["Cancelled<br/>any time before Billed<br/>trips freed, reference kept"]
        DONE --> BD
        CHG --> BD
        BD --> LCK
        PAP -.-> LCK
        LCK --> FWD
        FWD --> SAS
        FWD -.-> XLS
        SAS --> BLD
        BD -->|"cancel, with reason"| CAN
    end

    class RA,DP,BK,SO,PEN,Q1,HOLD,CON,LOST,BB,CP,TRIP,PR,TDS,TCAN,CR,WH,DROP,OUT,GAR,DONE,COR,OOD,BD,CHG,LCK,FWD,BLD,CAN built
    class PAP,XLS missing
    class MSG,SAS outside
```

Sources: `Booking.cs`, `Booking.RateAgreement.cs`, `CreateTripFromBookingHandler`, `CrewAssignment`,
`Trip.Status.cs`, `Trip.Corrections.cs`, `Trip.Pricing.cs`, `RatePricer`, `BillingBatch.Status.cs`.
Known gap: every drop's load still gets its own draft load invoice, which Cold Chain doesn't use.

## 3. Freight forwarding

Domestic, import, export, triangular and reefer files, plus trucking files. The workfile type decides
its reference series and rules (`WorkfileRules`, `WorkfileSeries`).

```mermaid
flowchart TD
    classDef built fill:#dcfce7,stroke:#16a34a,color:#14532d
    classDef partial fill:#fef3c7,stroke:#d97706,color:#78350f
    classDef missing fill:#fee2e2,stroke:#dc2626,color:#7f1d1d,stroke-dasharray:5 5
    classDef outside fill:#f3f4f6,stroke:#9ca3af,color:#374151,stroke-dasharray:5 5

    subgraph QUOTE["Quotation"]
        TAR["Tariffs + charge codes"]
        QD["Quotation: Draft<br/>customer, currency, lines"]
        QQ{"Approved?"}
        QR["Rejected<br/>reason required"]
        QA["Approved"]
        TAR --> QD
        QD --> QQ
        QQ -->|"no"| QR
        QQ -->|"yes"| QA
    end

    subgraph BOOKING["Booking"]
        BP["Convert to booking<br/>one booking per quotation<br/>Pending"]
        BC["Confirmed<br/>carrier reference"]
        BE["In execution"]
        BCO["Completed"]
        BX["Cancelled"]
        LD["Loads against the booking<br/>trucking legs"]
        QA --> BP
        BP --> BC
        BP --> BX
        BC --> BX
        BC --> BE
        BE --> BCO
        BC -.-> LD
    end

    subgraph WORKFILE["Workfile"]
        PRO["Promote booking to workfile<br/>one per booking, one way<br/>BKN-DOM / IMP / EXP / TRI / RFV / TRK number"]
        OPEN["Open<br/>ops fill the header: partners, vessel,<br/>BL, containers, milestones<br/>reefer: genset add-on"]
        MR["Manager review"]
        AR["Accounting review"]
        CL["Closed"]
        WX["Cancelled"]
        LOCK["Reference closure requested<br/>file frozen; only Accounting releases it<br/>Close still allowed"]
        CHG["Workfile charges<br/>forwarded to Accounting"]
        SAS["Accounting bills in SAS"]
        BC --> PRO
        PRO --> OPEN
        OPEN -->|"submit for approval"| MR
        MR -->|"disapprove"| OPEN
        MR -->|"sign off"| AR
        AR -->|"close"| CL
        OPEN -->|"cancel"| WX
        MR -->|"cancel"| WX
        OPEN -.-> LOCK
        AR -.-> CHG
        CHG -.-> SAS
    end

    class TAR,QD,QQ,QR,QA,BP,BC,BE,BCO,BX,PRO,OPEN,MR,AR,CL,WX,LOCK built
    class LD partial
    class CHG missing
    class SAS outside
```

Sources: `Quotation.cs`, `Booking.cs`, `LoadService.CreateLoadsAsync`, `Workfile.Status.cs`,
`WorkfileStatusMachine`. Known gaps: loads can be tied to a booking through the API but there is no
screen for it; workfiles have no charges yet, so "forward to Accounting" moves the file without any
amounts; Accounting has no way to send a file back.

## 4. Trucking

The original core: loads and trips, with the driver app recording each stop.

```mermaid
flowchart TD
    classDef built fill:#dcfce7,stroke:#16a34a,color:#14532d
    classDef partial fill:#fef3c7,stroke:#d97706,color:#78350f
    classDef missing fill:#fee2e2,stroke:#dc2626,color:#7f1d1d,stroke-dasharray:5 5
    classDef outside fill:#f3f4f6,stroke:#9ca3af,color:#374151,stroke-dasharray:5 5

    subgraph PLAN["Plan"]
        LD["Load created<br/>customer, pickup, delivery, rate<br/>Load: Draft"]
        INV["Draft invoice<br/>made with the load"]
        WHO{"Who picks<br/>the truck?"}
        TR["Trip created<br/>truck, driver, stops in order<br/>Trip: Draft"]
        LD --> INV
        LD --> WHO
        WHO -->|"dispatcher"| TR
        WHO -->|"AI dispatch agent<br/>checks hours of service"| TR
    end

    subgraph RUN["Run"]
        DS["Trip: Dispatched<br/>Load: Dispatched"]
        APP["Driver app<br/>arrive / depart at each stop<br/>live GPS tracking"]
        PU["Load: Picked up<br/>Trip: In transit"]
        AT{"At the<br/>drop-off"}
        DEL["Delivered<br/>proof of delivery uploaded"]
        FT["Foul trip or<br/>Cancelled by client"]
        TC["Trip: Completed<br/>every drop departed with an outcome"]
        TX["Trip cancelled<br/>its loads cancelled"]
        CUS["Customer portal<br/>tracking link, documents"]
        TR -->|"dispatch"| DS
        DS --> APP
        APP --> PU
        PU --> AT
        AT --> DEL
        AT --> FT
        DEL --> TC
        FT --> TC
        TR -.->|"cancel, any time before completed"| TX
        DS -.-> CUS
    end

    subgraph GETPAID["Get paid"]
        SEND["Invoice sent<br/>email + payment link"]
        PAY{"Customer<br/>pays"}
        PD["Paid / Partially paid"]
        QBO["QuickBooks sync<br/>if connected"]
        INV --> SEND
        TC --> SEND
        SEND --> PAY
        PAY -->|"Stripe checkout"| PD
        PAY -->|"recorded by the office"| PD
        PD --> QBO
    end

    class LD,INV,WHO,TR,DS,APP,PU,AT,DEL,FT,TC,TX,CUS,SEND,PAY,PD,QBO built
```

Sources: `Load`, `LoadFactory.CreateInvoice`, `Trip.Status.cs`, `TripStatusMachine`, `Invoice.cs`.
At Skybest the driver app isn't used yet: drivers report by Messenger and the office records the trip
(see the Cold Chain chart).
